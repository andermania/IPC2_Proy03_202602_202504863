using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sat.Web.Pages;

/// <summary>Ayuda / Información del estudiante.</summary>
public class EstudianteModel : PageModel
{
    public const string Nombre = "Kevin Ánderson Montejo Martínez";
    public const string Carnet = "202504863";
    public const string Curso = "IPC2 - Introducción a la Programación y Computación 2";
    public const string Proyecto = "Proyecto 3 - API de autorización de DTE de la SAT";
    public const string Repositorio = "https://github.com/andermania/IPC2_Proy03_202602_202504863";

    public void OnGet()
    {
    }
}
