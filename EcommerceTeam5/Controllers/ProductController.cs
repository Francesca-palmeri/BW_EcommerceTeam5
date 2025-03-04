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
                string query = "SELECT prodottoId, Nome, Descrizione, Prezzo, ImageURL, Creato, NomeCategoria FROM Prodotti INNER JOIN Categorie ON Prodotti.CategoriaID = Categorie.CategoriaID;";

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
                                CategoriaNome = reader.GetString(6),
                                
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
                string query = "SELECT prodottoId, Nome, Descrizione, Prezzo, ImageURL, Creato, NomeCategoria FROM Prodotti INNER JOIN Categorie ON Prodotti.CategoriaID = Categorie.CategoriaID WHERE prodottoId = @Id;";

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
                                CategoriaNome = reader.GetString(6),
                                
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

        public async Task<IActionResult> Cart(int utenteId)
        {
            var carrello = new Carrello
            {
                Products = new List<Product>()
            };

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"
            SELECT p.prodottoId, p.Nome, p.Descrizione, p.Prezzo, p.ImageURL, c.TotaleCarrello
            FROM Carrello c
            INNER JOIN Prodotti p ON c.UtenteID = @UtenteId AND c.OrdineID IS NULL
            INNER JOIN Ordini o ON c.OrdineID = o.OrdineID
            WHERE c.UtenteID = @UtenteId";

                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UtenteId", utenteId);

                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            carrello.Products.Add(new Product()
                            {
                                Id = reader.GetInt32(0),
                                Nome = reader.GetString(1),
                                Descrizione = reader.GetString(2),
                                Prezzo = reader.GetDecimal(3),
                                Immagine = reader.GetString(4)
                            });
                            carrello.TotaleCarrello = reader.GetDecimal(5);
                        }
                    }
                }
            }

            return View(carrello);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int id)
        {
            int utenteId = 1; // Puoi ottenere l'utente ID dal sistema di autenticazione se esistente

            // Connessione al database per aggiungere il prodotto al carrello
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Controlla se esiste giÃ un carrello aperto per l'utente
                string checkCartQuery = "SELECT CarrelloID FROM Carrello WHERE UtenteID = @UtenteId AND OrdineID IS NULL;";
                int carrelloId;

                await using (SqlCommand checkCartCommand = new SqlCommand(checkCartQuery, connection))
                {
                    checkCartCommand.Parameters.AddWithValue("@UtenteId", utenteId);
                    var result = await checkCartCommand.ExecuteScalarAsync();

                    if (result == null)
                    {
                        // Se non esiste, crea un nuovo carrello
                        string createCartQuery = "INSERT INTO Carrello (UtenteID, NumeroProdotti, TotaleCarrello) OUTPUT INSERTED.CarrelloID VALUES (@UtenteId, 0, 0);";
                        await using (SqlCommand createCartCommand = new SqlCommand(createCartQuery, connection))
                        {
                            createCartCommand.Parameters.AddWithValue("@UtenteId", utenteId);
                            carrelloId = (int)await createCartCommand.ExecuteScalarAsync();
                        }
                    }
                    else
                    {
                        carrelloId = (int)result;
                    }
                }

                // Aggiungi il prodotto al carrello
                string addToCartQuery = @"
            UPDATE Carrello 
            SET NumeroProdotti = NumeroProdotti + 1, TotaleCarrello = TotaleCarrello + (SELECT Prezzo FROM Prodotti WHERE prodottoId = @ProdottoId) 
            WHERE CarrelloID = @CarrelloID;
        ";

                await using (SqlCommand addToCartCommand = new SqlCommand(addToCartQuery, connection))
                {
                    addToCartCommand.Parameters.AddWithValue("@ProdottoId", id);
                    addToCartCommand.Parameters.AddWithValue("@CarrelloID", carrelloId);
                    await addToCartCommand.ExecuteNonQueryAsync();
                }
            }

            // Reindirizza alla pagina del carrello
            return RedirectToAction("Cart", new { utenteId = utenteId });
        

    }
    }
}