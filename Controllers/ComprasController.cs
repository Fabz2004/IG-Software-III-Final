

using ALODAN.Datos;
using ALODAN.Helpers;
using ALODAN.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ALODAN.Controllers
{
    public class ComprasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ComprasController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // 🔹 Vista de compras del usuario logueado
        public IActionResult Index()
        {
            var usuarioJson = HttpContext.Session.GetString("UsuarioLogueado");

            if (usuarioJson == null)
            {
                ViewBag.UsuarioNoLogueado = true;
                return View(new List<Pedido>());
            }

            var usuario = JsonSerializer.Deserialize<Usuario>(usuarioJson);

            var pedidos = _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(p => p.UsuarioId == usuario.Id)
                .OrderByDescending(p => p.FechaPedido)
                .ToList();

            return View(pedidos);
        }

        // 🔹 Confirmar compra → guarda pedido y envía correo con constancia PDF
        [HttpPost]
        public async Task<IActionResult> ConfirmarCompra(string NumeroTarjeta, string FechaExpiracion, string CVV)
        {
            // ========================================
            // ✅ VALIDACIONES DE PAGO
            // ========================================

            // 🔹 Remover espacios del número de tarjeta
            NumeroTarjeta = NumeroTarjeta?.Replace(" ", "") ?? "";

            // ✅ Validar número de tarjeta (16 dígitos)
            if (string.IsNullOrWhiteSpace(NumeroTarjeta) ||
                NumeroTarjeta.Length != 16 ||
                !NumeroTarjeta.All(char.IsDigit))
            {
                TempData["ErrorCheckout"] = "El número de tarjeta debe tener exactamente 16 dígitos.";
                return RedirectToReferrerOrCheckout();
            }

            // ✅ Validar CVV (3 dígitos)
            if (string.IsNullOrWhiteSpace(CVV) ||
                CVV.Length != 3 ||
                !CVV.All(char.IsDigit))
            {
                TempData["ErrorCheckout"] = "El CVV debe tener exactamente 3 dígitos.";
                return RedirectToReferrerOrCheckout();
            }

            // ✅ Validar formato de fecha (MM/AA)
            if (string.IsNullOrWhiteSpace(FechaExpiracion) ||
                !System.Text.RegularExpressions.Regex.IsMatch(FechaExpiracion, @"^\d{2}/\d{2}$"))
            {
                TempData["ErrorCheckout"] = "La fecha debe tener el formato MM/AA (ejemplo: 12/25).";
                return RedirectToReferrerOrCheckout();
            }

            // ✅ Validar que la fecha sea válida (mes entre 01-12)
            var fechaParts = FechaExpiracion.Split('/');
            if (!int.TryParse(fechaParts[0], out int mes) || mes < 1 || mes > 12)
            {
                TempData["ErrorCheckout"] = "El mes debe estar entre 01 y 12.";
                return RedirectToReferrerOrCheckout();
            }

            // ✅ Validar que el año sea válido
            if (!int.TryParse(fechaParts[1], out int anio))
            {
                TempData["ErrorCheckout"] = "El año ingresado no es válido.";
                return RedirectToReferrerOrCheckout();
            }

            // ✅ Validar que la tarjeta no esté vencida
            int anioCompleto = 2000 + anio; // Convertir AA a 20AA
            var fechaExpiracion = new DateTime(anioCompleto, mes, 1).AddMonths(1).AddDays(-1); // Último día del mes

            if (fechaExpiracion < DateTime.Now)
            {
                TempData["ErrorCheckout"] = "La tarjeta está vencida. Por favor, utiliza otra tarjeta.";
                return RedirectToReferrerOrCheckout();
            }

            // ========================================
            // ✅ SI TODAS LAS VALIDACIONES PASAN, CONTINUAR CON LA COMPRA
            // ========================================

            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            if (!carrito.Any())
                return RedirectToAction("Index", "Carrito");

            var usuarioJson = HttpContext.Session.GetString("UsuarioLogueado");
            if (usuarioJson == null)
                return RedirectToAction("Perfil", "Cuenta");

            var usuario = JsonSerializer.Deserialize<Usuario>(usuarioJson);

            // Crear pedido
            // Obtener el último número de pedido del usuario
            var ultimoNumero = _context.Pedidos
                .Where(p => p.UsuarioId == usuario.Id)
                .OrderByDescending(p => p.NumeroPedido)
                .Select(p => p.NumeroPedido)
                .FirstOrDefault();

            var pedido = new Pedido
            {
                UsuarioId = usuario.Id,
                FechaPedido = DateTime.Now,
                Estado = "Solicitud recibida",
                Total = carrito.Sum(c => c.Subtotal),
                NumeroPedido = ultimoNumero + 1,
                Detalles = carrito.Select(c => new PedidoDetalle
                {
                    ProductoId = c.ProductoId,
                    Cantidad = c.Cantidad,
                    PrecioUnitario = c.Precio,
                    Talla = c.Talla,
                    Color = c.Color
                }).ToList()
            };

            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            // 🔹 Volver a cargar el pedido con los productos incluidos
            pedido = _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefault(p => p.Id == pedido.Id);

            // Eliminar carrito
            HttpContext.Session.Remove("Carrito");

            // Enviar correo con constancia PDF
            await EnviarCorreoConstancia(usuario, pedido);

            return RedirectToAction("Index");
        }

        // 🔹 Método auxiliar para redirigir a la página anterior
        private IActionResult RedirectToReferrerOrCheckout()
        {
            var referer = Request.GetTypedHeaders().Referer?.ToString();

            if (!string.IsNullOrEmpty(referer) &&
                Uri.TryCreate(referer, UriKind.Relative, out _) &&
                Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }

            // Por defecto, ir al checkout
            return RedirectToAction("Index", "Checkout");
        }

        // 🔹 Ver estado de envío
        public IActionResult EstadoEnvio(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Solicitud inválida.");
            }
            var pedido = _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // ================================
        // 🔹 Método privado para enviar correo
        // ================================
        private async Task EnviarCorreoConstancia(Usuario usuario, Pedido pedido)
        {
            var emailSettings = _config.GetSection("EmailSettings");
            var remitente = emailSettings["Email"];
            var password = emailSettings["Password"];
            var host = emailSettings["Host"];
            var puerto = int.Parse(emailSettings["Port"]);

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("ALODAN", remitente));
            mensaje.To.Add(MailboxAddress.Parse(usuario.Email));
            mensaje.Subject = "Constancia de compra - ALODAN";

            // ===============================
            // 🧮 Calcular subtotal, descuento y total
            // ===============================
            decimal subtotal = pedido.Detalles.Sum(d => d.PrecioUnitario * d.Cantidad);
            decimal descuento = 0;
            string porcentajeTexto = "";

            if (subtotal >= 300)
            {
                descuento = subtotal * 0.15m;
                porcentajeTexto = "15%";
            }
            else if (subtotal >= 200)
            {
                descuento = subtotal * 0.10m;
                porcentajeTexto = "10%";
            }
            else if (subtotal >= 100)
            {
                descuento = subtotal * 0.05m;
                porcentajeTexto = "5%";
            }

            decimal totalFinal = subtotal - descuento;

            // ===============================
            // 📨 Cuerpo del mensaje
            // ===============================
            var cuerpo = new BodyBuilder();

            string mensajeTexto =
                $"Hola {usuario.Nombre},\n\n" +
                $"Gracias por tu compra en ALODAN 💖.\n\n" +
                $"🧾 Resumen de tu compra:\n" +
                $"Fecha: {pedido.FechaPedido:dd/MM/yyyy}\n" +
                $"Subtotal: S/. {subtotal:N2}\n";

            if (descuento > 0)
                mensajeTexto += $"Descuento aplicado ({porcentajeTexto}): -S/. {descuento:N2}\n";

            mensajeTexto +=
                $"Total pagado: S/. {totalFinal:N2}\n\n" +
                $"Adjuntamos tu constancia de compra en formato PDF.\n\n" +
                $"Pronto recibirás más información sobre tu pedido.\n\n" +
                $"Gracias por elegir ALODAN 🌷";

            cuerpo.TextBody = mensajeTexto;

            // Generar y adjuntar PDF
            var pdfBytes = GenerarPdfConstancia(usuario, pedido);
            cuerpo.Attachments.Add("Constancia_ALODAN.pdf", pdfBytes, new ContentType("application", "pdf"));

            mensaje.Body = cuerpo.ToMessageBody();

            // ===============================
            // ✉️ Envío del correo
            // ===============================
            using (var smtp = new SmtpClient())
            {
                await smtp.ConnectAsync(host, puerto, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(remitente, password);
                await smtp.SendAsync(mensaje);
                await smtp.DisconnectAsync(true);
            }
        }

        private byte[] GenerarPdfConstancia(Usuario usuario, Pedido pedido)
        {
            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                // ===============================
                // 🧮 Calcular subtotal y descuento
                // ===============================
                decimal subtotal = pedido.Detalles.Sum(d => d.PrecioUnitario * d.Cantidad);
                decimal descuento = 0;
                string porcentajeTexto = "";

                if (subtotal >= 300)
                {
                    descuento = subtotal * 0.15m;
                    porcentajeTexto = "15%";
                }
                else if (subtotal >= 200)
                {
                    descuento = subtotal * 0.10m;
                    porcentajeTexto = "10%";
                }
                else if (subtotal >= 100)
                {
                    descuento = subtotal * 0.05m;
                    porcentajeTexto = "5%";
                }

                decimal totalFinal = subtotal - descuento;

                // ===============================
                // 🧾 Contenido del PDF
                // ===============================
                writer.WriteLine("             🖤 ALODAN - Constancia de Compra 🖤");
                writer.WriteLine("-----------------------------------------------------");
                writer.WriteLine($"Cliente: {usuario.Nombre}");
                writer.WriteLine($"Correo: {usuario.Email}");
                writer.WriteLine($"Fecha de compra: {pedido.FechaPedido:dd/MM/yyyy}");
                writer.WriteLine();
                writer.WriteLine("Detalles del pedido:");
                writer.WriteLine();

                foreach (var item in pedido.Detalles)
                {
                    var nombreProducto = item.Producto?.Nombre ?? "Producto desconocido";
                    writer.WriteLine($"- {nombreProducto} ({item.Talla}, {item.Color}) x{item.Cantidad} - S/. {item.PrecioUnitario:N2}");
                }

                writer.WriteLine();
                writer.WriteLine("-----------------------------------------------------");
                writer.WriteLine($"Subtotal: S/. {subtotal:N2}");
                if (descuento > 0)
                    writer.WriteLine($"Descuento aplicado ({porcentajeTexto}): -S/. {descuento:N2}");
                writer.WriteLine($"Total pagado: S/. {totalFinal:N2}");
                writer.WriteLine("-----------------------------------------------------");
                writer.WriteLine();
                writer.WriteLine("💌 ¡Gracias por tu compra en ALODAN!");
                writer.WriteLine("Esperamos verte pronto 💫");
                writer.Flush();
            }

            return stream.ToArray();
        }
    }
}