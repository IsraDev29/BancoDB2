using Microsoft.AspNetCore.Mvc;
using FinalProject.Models;
using System.Linq;

namespace FinalProject.Controllers
{
    public class ClientesController : Controller
    {
        // Variable privada para almacenar el contexto de la base de datos
        private readonly BancoContext _context;

        // El constructor recibe la conexión automáticamente gracias a la línea que agregaste en Program.cs
        public ClientesController(BancoContext context)
        {
            _context = context;
        }

        // Método para mostrar la página principal de clientes
        public IActionResult Index()
        {
            // Esto equivale a hacer un "SELECT * FROM Clientes" de forma automática
            var listaClientes = _context.Clientes.ToList();

            // Enviamos la lista a la interfaz web
            return View(listaClientes);
        }
    }
}