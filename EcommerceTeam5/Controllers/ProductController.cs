using Microsoft.AspNetCore.Mvc;
using EcommerceTeam5.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Data;

namespace EcommerceTeam5.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class ProductController : Controller
    {
        private readonly string _connectionString;

        // Iniezione della configurazione tramite il costruttore
        public ProductController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Azione per visualizzare tutti i prodotti
        public async Task<IActionResult> Index()
        {
            var productsList = new Magazzino()
            {
                Products = new List<Product>()
            };

            // Connessione al database per ottenere i prodotti
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT ProdottoId, Nome, Descrizione, Prezzo, ImageURL, Creato, NomeCategoria FROM Prodotti INNER JOIN Categorie ON Prodotti.CategoriaID = Categorie.CategoriaID;";

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
                                NomeCategoria = reader.GetString(6),
                                
                            });
                        }
                    }
                }
            }

            return View(productsList);
        }

        // Azione per visualizzare i dettagli di un singolo prodotto
        [HttpGet("Product/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            Product product = null;

            // Connessione al database per ottenere i dettagli del prodotto
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT ProdottoId, Nome, Descrizione, Prezzo, ImageURL, Creato, NomeCategoria FROM Prodotti INNER JOIN Categorie ON Prodotti.CategoriaID = Categorie.CategoriaID WHERE ProdottoId = @Id;";

                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            product = new Product()
                            {
                                Id = reader.GetInt32(0),
                                Nome = reader.GetString(1),
                                Descrizione = reader.GetString(2),
                                Prezzo = reader.GetDecimal(3),
                                Immagine = reader.GetString(4),
                                Creazione = reader.GetDateTime(5),
                                NomeCategoria = reader.GetString(6),
                                
                            };
                        }
                    }
                }
            }
            
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Mostra la pagina Admin con il form e la lista dei prodotti
        [HttpGet]
        public async Task<IActionResult> Admin()
        {
            var magazzino = new Magazzino { Products = new List<Product>() };
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    SELECT p.ProdottoID, p.Nome, p.Descrizione, p.Prezzo, p.ImageURL, p.Creato, p.CategoriaID, c.NomeCategoria
                    FROM Prodotti p
                    INNER JOIN Categorie c ON p.CategoriaID = c.CategoriaID;";
                await using (SqlCommand command = new SqlCommand(query, connection))
                await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        magazzino.Products.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Nome = reader.GetString(1),
                            Descrizione = reader.GetString(2),
                            Prezzo = reader.GetDecimal(3),
                            Immagine = reader.GetString(4),
                            Creazione = reader.GetDateTime(5),
                            CategoriaID = reader.GetInt32(6),
                            NomeCategoria = reader.GetString(7)
                        });
                    }
                }
            }
            return View(magazzino);
        }

        // POST: Gestisce sia la creazione che la modifica, in base al valore dell'Id
        [HttpPost]
        public async Task<IActionResult> Admin(Product product)
        {
            if (!ModelState.IsValid)
            {
                // Se ci sono errori, ricarica la view con la lista dei prodotti
                return await Admin();
            }

            // Se Id Ã¨ 0, si tratta di una creazione
            if (product.Id == 0)
            {
                if (product.Creazione == DateTime.MinValue)
                    product.Creazione = DateTime.Now;

                await using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        INSERT INTO Prodotti (Nome, Descrizione, Prezzo, ImageURL, Creato, CategoriaID)
                        VALUES (@Nome, @Descrizione, @Prezzo, @ImageURL, @Creato, @CategoriaID);";
                    await using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nome", product.Nome);
                        command.Parameters.AddWithValue("@Descrizione", product.Descrizione);
                        command.Parameters.AddWithValue("@Prezzo", product.Prezzo);
                        command.Parameters.AddWithValue("@ImageURL", product.Immagine ?? "");
                        command.Parameters.AddWithValue("@Creato", product.Creazione);
                        command.Parameters.AddWithValue("@CategoriaID", product.CategoriaID);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                // Altrimenti, si tratta di una modifica
                await using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        UPDATE Prodotti
                        SET Nome = @Nome,
                            Descrizione = @Descrizione,
                            Prezzo = @Prezzo,
                            ImageURL = @ImageURL,
                            Creato = @Creato,
                            CategoriaID = @CategoriaID
                        WHERE ProdottoID = @Id;";
                    await using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nome", product.Nome);
                        command.Parameters.AddWithValue("@Descrizione", product.Descrizione);
                        command.Parameters.AddWithValue("@Prezzo", product.Prezzo);
                        command.Parameters.AddWithValue("@ImageURL", product.Immagine ?? "");
                        command.Parameters.AddWithValue("@Creato", product.Creazione);
                        command.Parameters.AddWithValue("@CategoriaID", product.CategoriaID);
                        command.Parameters.AddWithValue("@Id", product.Id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            return RedirectToAction("Admin");
        }

        // POST: Gestisce la cancellazione del prodotto
        [HttpPost]
        public async Task<IActionResult> AdminDelete(int Id)
        {
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Prodotti WHERE ProdottoID = @Id;";
                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", Id);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return RedirectToAction("Admin");
        }
    }
}