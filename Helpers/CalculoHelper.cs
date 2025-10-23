using ALODAN.Models;
using System.Collections.Generic;
using System.Linq;

namespace ALODAN.Helpers
{
    public static class CalculoHelper
    {
        public static decimal CalcularSubtotal(List<CarritoItem> carrito)
        {
            return carrito?.Sum(c => c.Subtotal) ?? 0m;
        }

        public static decimal CalcularTotal(List<CarritoItem> carrito)
        {
            var subtotal = CalcularSubtotal(carrito);
            var descuento = DescuentoHelper.Calcular(subtotal);
            return subtotal - descuento;
        }
    }
}
