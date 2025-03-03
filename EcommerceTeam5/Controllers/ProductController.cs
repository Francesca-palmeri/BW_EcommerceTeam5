﻿using Microsoft.AspNetCore.Mvc;
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
    }
}