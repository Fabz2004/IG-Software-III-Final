namespace ALODAN.Helpers
{
    public static class DescuentoHelper
    {
        public static decimal Calcular(decimal subtotal)
        {
            if (subtotal < 0)
                return 0m;

            if (subtotal >= 300)
                return subtotal * 0.15m;
            if (subtotal >= 200)
                return subtotal * 0.10m;
            if (subtotal >= 100)
                return subtotal * 0.05m;

            return 0m;
        }
    }
}