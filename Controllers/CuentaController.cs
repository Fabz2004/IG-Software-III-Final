

using ALODAN.Datos;
using ALODAN.Helpers;
using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Alodan.Controllers
{
    public class CuentaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string SessionUsuario = "UsuarioLogueado";

        public CuentaController(ApplicationDbContext context)
        {
            _context = context;
        }

        //🔹 LOGIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string Email, string Password)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == Email && u.Password == Password);

            if (usuario == null)
            {
              
                TempData["ErrorLogin"] = "Correo o contrasena incorrectos.";

                
                var returnUrl = HttpContext.Session.GetString("ReturnUrl");

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return RedirectToAction("Index", "Carrito");
                }
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                {
                    return Redirect(referer);
                }

                return RedirectToAction("Index", "Carrito");
            }

            // ✅ Si las credenciales son correctas
            HttpContext.Session.SetString("UsuarioLogueado", JsonSerializer.Serialize(usuario));

            var returnUrlSuccess = HttpContext.Session.GetString("ReturnUrl");
            if (!string.IsNullOrEmpty(returnUrlSuccess))
            {
                HttpContext.Session.Remove("ReturnUrl");
                return Redirect(returnUrlSuccess);
            }

            return RedirectToAction("Perfil");
        }




        // 🔹 REGISTRO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registro(Usuario nuevoUsuario)
        {
            if (nuevoUsuario == null)
            {
                TempData["ErrorRegistro"] = "Error al procesar el registro. Intenta nuevamente.";
                return RedirectToReferrerOrCarrito();
            }

            // ✅ Validar formato de correo electrónico
            var emailRegex = new Regex(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");
            if (string.IsNullOrWhiteSpace(nuevoUsuario.Email) || !emailRegex.IsMatch(nuevoUsuario.Email))
            {
                TempData["ErrorRegistro"] = "Ingresa un correo electronico valido (por ejemplo: nombre@dominio.com).";
                return RedirectToReferrerOrCarrito();
            }

            // ✅ Validar contraseña mínima 8 caracteres
            if (string.IsNullOrWhiteSpace(nuevoUsuario.Password) || nuevoUsuario.Password.Length < 8)
            {
                TempData["ErrorRegistro"] = "La contrasena debe tener al menos 8 caracteres.";
                return RedirectToReferrerOrCarrito();
            }

            // ✅ Validar teléfono (9 dígitos)
            if (string.IsNullOrWhiteSpace(nuevoUsuario.Telefono) ||
                nuevoUsuario.Telefono.Length != 9 ||
                !nuevoUsuario.Telefono.All(char.IsDigit))
            {
                TempData["ErrorRegistro"] = "El numero de telefono debe tener exactamente 9 digitos.";
                return RedirectToReferrerOrCarrito();
            }

            // ✅ Validar correo duplicado
            if (_context.Usuarios.Any(u => u.Email == nuevoUsuario.Email))
            {
                TempData["ErrorRegistro"] = "Este correo ya esta registrado. Por favor, utiliza otro.";
                return RedirectToReferrerOrCarrito();
            }

            // ✅ Guardar usuario
            _context.Usuarios.Add(nuevoUsuario);
            _context.SaveChanges();

            // ✅ Iniciar sesión automáticamente
            HttpContext.Session.SetString("UsuarioLogueado", JsonSerializer.Serialize(nuevoUsuario));
            TempData["RegistroExitoso"] = true;

            // ✅ Verificar de dónde viene el usuario
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>("Carrito");

            // Si tiene carrito, ir al checkout
            if (carrito != null && carrito.Any())
            {
                return RedirectToAction("Index", "Checkout");
            }

            // Si no tiene carrito, verificar el referer
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                return Redirect(referer);
            }

            // Por defecto, ir a inicio de productos
            return RedirectToAction("Inicio", "Productos");
        }

        // 🔹 Método auxiliar para redirigir a la página anterior
        private IActionResult RedirectToReferrerOrCarrito()
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                return Redirect(referer);
            }

            // Por defecto, ir al carrito si no hay referer
            return RedirectToAction("Index", "Carrito");
        }

        // 🔹 Verificar correo duplicado (AJAX)
        [HttpGet]
        public JsonResult VerificarCorreo(string email)
        {
            bool existe = _context.Usuarios.Any(u => u.Email == email);
            return Json(existe);
        }

        // 🔹 PERFIL
        public IActionResult Perfil()
        {
            var usuarioJson = HttpContext.Session.GetString(SessionUsuario);
            if (usuarioJson == null)
                return View("SinSesion");

            var usuario = JsonSerializer.Deserialize<Usuario>(usuarioJson);
            return View(usuario);
        }

        // 🔹 LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Inicio", "Productos");
        }
    }
}
