using Microsoft.AspNetCore.Mvc;

namespace CalculatorApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult OnPostSum(string Number1, string Number2)
    {
        ViewData["Number1"] = Number1;
        ViewData["Number2"] = Number2;

        if (!int.TryParse(Number1, out int number1))
        {
            ModelState.AddModelError("Number1", "Number1 должно быть целым числом.");
        }

        if (!int.TryParse(Number2, out int number2))
        {
            ModelState.AddModelError("Number2", "Number2 должно быть целым числом.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index");
        }

        ViewData["Result"] = number1 + number2;
        return View("Index");
    }
}
