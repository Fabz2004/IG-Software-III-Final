using ALODAN.Datos;
using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ALODAN.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 Página de categoría (mantienes igual)

        public IActionResult Inicio()
        {
            return View();
        }
        public IActionResult Index(string categoria)
        {
            var productos = string.IsNullOrEmpty(categoria)
                ? _context.Productos.ToList()
                : _context.Productos.Where(p => p.Categoria == categoria).ToList();

            ViewBag.Categoria = categoria;
            return View(productos);
        }

        // 🔹 Acción para buscar con filtros
        [HttpGet]
   
        public IActionResult Buscar(string busqueda, string talla, decimal? precioMin, decimal? precioMax)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorBusqueda = "Los parámetros de búsqueda no son válidos.";
                return View(new List<Producto>());
            }
            var productos = _context.Productos.AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
            {
                var texto = busqueda.ToLower().Trim();
                productos = productos.Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.Categoria.ToLower().Contains(texto) ||
                    p.Descripcion.ToLower().Contains(texto));
            }

            if (!string.IsNullOrEmpty(talla))
                productos = productos.Where(p => p.Talla.ToLower().Contains(talla.ToLower()));

            if (precioMin.HasValue)
                productos = productos.Where(p => p.Precio >= precioMin.Value);

            if (precioMax.HasValue)
                productos = productos.Where(p => p.Precio <= precioMax.Value);

            var lista = productos.ToList();
            if (!lista.Any())
                ViewBag.SinResultados = true;

            return View("ResultadosBusqueda", lista);
        }


        // 🔹 Detalles (sin cambios)
        public IActionResult Detalles(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Solicitud inválida.");
            }
            var producto = _context.Productos.FirstOrDefault(p => p.Id == id);
            if (producto == null) return NotFound();

            return View(producto);
        }
    }
}
