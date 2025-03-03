using Microsoft.AspNetCore.Mvc;
using EcommerceTeam5.Models;
using Microsoft.Data.SqlClient;
using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EcommerceTeam5.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class ProductController : Controller
    { 
        private readonly String _connectionString;

        public ProductController() { 

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();
            
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            var productsList = new Magazzino()
            {
                Products = new List<Product>()
            };

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT Prodotti.prodottoId, Prodotti.Nome, Prodotti.Descrizione, Prodotti.Prezzo, Prodotti.ImmageURL, Prodotti.Creato, Prodotti.CategoriaId;";
            
                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            productsList.Products.Add(new Product()
                            {
                                Id = reader.GetInt32(0),
                                Nome = reader.GetString(1),
                                Descrizione = reader.GetString(2),
                                Prezzo = reader.GetDecimal(3),
                                Immagine = reader.GetString(4),
                                Creazione = reader.GetDateTime(5),
                                Categoria = reader.GetString(6)
                            });
                        }
                    }
                } 
            }

            return View(productsList);
        }

        [HttpGet("Product/{id}")]
    }
}
