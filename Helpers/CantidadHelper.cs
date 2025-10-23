using ALODAN.Models;
using System.Collections.Generic;
using System.Linq;

namespace ALODAN.Helpers
{
    public static class CantidadHelper
    {
        public static void Actualizar(List<CarritoItem> carrito, int productoId, string accion)
        {
            var item = carrito?.FirstOrDefault(c => c.ProductoId == productoId);
            if (item == null) return;

            if (accion == "sumar")
                item.Cantidad++;
            else if (accion == "restar" && item.Cantidad > 1)
                item.Cantidad--;
        }

        public static int CalcularCantidadTotal(List<CarritoItem> carrito)
        {
            if (carrito == null || !carrito.Any()) return 0;
            return carrito.Where(c => c.Cantidad > 0).Sum(c => c.Cantidad);
        }
    }
}
