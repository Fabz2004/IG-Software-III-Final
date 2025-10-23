using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

public class CheckoutController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var usuarioJson = HttpContext.Session.GetString("UsuarioLogueado");
        if (string.IsNullOrEmpty(usuarioJson))
        {
            // Guarda a dónde querías ir
            HttpContext.Session.SetString("ReturnUrl", Url.Action("Index", "Checkout")!);

            // Mensaje bonito + abrir modal de login
            TempData["ErrorLogin"] = "Para finalizar la compra necesitas iniciar sesión.";
            return RedirectToAction("Cuenta", "Cuenta", new { mostrarLogin = true });
        }

        var usuario = JsonSerializer.Deserialize<Usuario>(usuarioJson);
        return View(usuario);
    }
}
