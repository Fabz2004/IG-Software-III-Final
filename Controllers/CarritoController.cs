using ALODAN.Datos;
using ALODAN.Helpers;
using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ALODAN.Controllers
{
    public class CarritoController : Controller
    {
        private const string CarritoSessionKey = "Carrito";
        private readonly ApplicationDbContext _context;

        public CarritoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(CarritoSessionKey) ?? new List<CarritoItem>();

            decimal subtotal = CalculoHelper.CalcularSubtotal(carrito);
            decimal descuento = DescuentoHelper.Calcular(subtotal);
            decimal totalFinal = subtotal - descuento;

            ViewBag.Subtotal = subtotal;
            ViewBag.Descuento = descuento;
            ViewBag.Total = totalFinal;
            ViewBag.CantidadCarrito = CantidadHelper.CalcularCantidadTotal(carrito);

            return View(carrito);
        }

        public IActionResult Agregar(int id, string talla, string color)
        {
            if (!ModelState.IsValid)
                return BadRequest("Datos inválidos.");


            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();

            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(CarritoSessionKey) ?? new List<CarritoItem>();

            var item = carrito.FirstOrDefault(c => c.ProductoId == id && c.Talla == talla && c.Color == color);
            if (item != null)
                item.Cantidad++;
            else
                carrito.Add(new CarritoItem
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    ImagenUrl = producto.ImagenUrl,
                    Precio = producto.Precio,
                    Cantidad = 1,
                    Talla = talla,
                    Color = color
                });

            HttpContext.Session.SetObject(CarritoSessionKey, carrito);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest("Datos inválidos.");
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(CarritoSessionKey) ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);

            if (item != null)
                carrito.Remove(item);

            HttpContext.Session.SetObject(CarritoSessionKey, carrito);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ProcederPago()
        {
            var usuarioJson = HttpContext.Session.GetString("UsuarioLogueado");
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(CarritoSessionKey) ?? new List<CarritoItem>();

            decimal subtotal = CalculoHelper.CalcularSubtotal(carrito);
            decimal descuento = DescuentoHelper.Calcular(subtotal);
            decimal totalFinal = subtotal - descuento;

            ViewBag.Subtotal = subtotal;
            ViewBag.Descuento = descuento;
            ViewBag.Total = totalFinal;

            if (usuarioJson == null)
            {
                HttpContext.Session.SetString("ReturnUrl", Url.Action("Index", "Checkout"));
                ViewBag.MostrarLogin = true;
                return View("Index", carrito);
            }

            return RedirectToAction("Index", "Checkout");
        }

        [HttpPost]
        public IActionResult ActualizarCantidad(int productoId, int nuevaCantidad)
        {
            if (!ModelState.IsValid)
                return BadRequest("Datos inválidos.");
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(CarritoSessionKey) ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == productoId);

            if (item != null && nuevaCantidad > 0)
                item.Cantidad = nuevaCantidad;

            HttpContext.Session.SetObject(CarritoSessionKey, carrito);
            return RedirectToAction("Index");
        }
    }
}
