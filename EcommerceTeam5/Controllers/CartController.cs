using Microsoft.AspNetCore.Mvc;
using EcommerceTeam5.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcommerceTeam5.Controllers
{
    public class CartController : Controller
    {
        private readonly string _connectionString;

        public CartController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Visualizza il carrello di un utente specifico
        public async Task<IActionResult> Index(int? utenteId)
        {
            if (utenteId == null)
            {
                utenteId = await EnsureUserExists(); // Assicura che l'utente esista
            }

            var cartItems = new List<CartItem>();
            decimal totaleCarrello = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    SELECT c.CarrelloID, c.UtenteID, c.ProdottoID, p.Nome, p.ImageURL, p.Prezzo, c.Quantita
                    FROM Carrello c
                    INNER JOIN Prodotti p ON c.ProdottoID = p.ProdottoID
                    WHERE c.UtenteID = @UtenteID;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UtenteID", utenteId);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new CartItem()
                            {
                                CarrelloID = reader.GetInt32(0),
                                UtenteID = reader.GetInt32(1),
                                ProdottoID = reader.GetInt32(2),
                                NomeProdotto = reader.GetString(3),
                                ImmagineProdotto = reader.GetString(4),
                                Prezzo = reader.GetDecimal(5),
                                Quantita = reader.GetInt32(6),
                            };

                            cartItems.Add(item);
                            totaleCarrello += item.Prezzo * item.Quantita;
                        }
                    }
                }
            }
            ViewData["TotaleCarrello"] = totaleCarrello; // Passa il totale alla view

            return View(cartItems);
        }

        // Aggiungere un prodotto al carrello
        public async Task<IActionResult> Aggiungi(int prodottoId, int quantita)
        {
            int utenteId = await EnsureUserExists();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    IF EXISTS (SELECT 1 FROM Carrello WHERE UtenteID = @UtenteID AND ProdottoID = @ProdottoID)
                    BEGIN
                        UPDATE Carrello SET Quantita = Quantita + @Quantita WHERE UtenteID = @UtenteID AND ProdottoID = @ProdottoID
                    END
                    ELSE
                    BEGIN
                        INSERT INTO Carrello (UtenteID, ProdottoID, Quantita) VALUES (@UtenteID, @ProdottoID, @Quantita)
                    END";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UtenteID", utenteId);
                    command.Parameters.AddWithValue("@ProdottoID", prodottoId);
                    command.Parameters.AddWithValue("@Quantita", quantita);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return RedirectToAction("Index", new { utenteId });
        }



        // Rimuovere un prodotto dal carrello
        public async Task<IActionResult> Rimuovi(int carrelloId, int utenteId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Carrello WHERE CarrelloID = @CarrelloID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CarrelloID", carrelloId);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return RedirectToAction("Index", new { utenteId });
        }

        // Svuotare il carrello
        public async Task<IActionResult> SvuotaCarrello(int utenteId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Carrello WHERE UtenteID = @UtenteID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UtenteID", utenteId);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return RedirectToAction("Index", new { utenteId });
        }



        // Conferma il carrello e crea un ordine
        public async Task<IActionResult> ConfermaOrdine(int? utenteId)
        {
            if (utenteId == null)
            {
                utenteId = await EnsureUserExists();
            }

            decimal totale = 0;
            List<CartItem> cartItems = new List<CartItem>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string selectQuery = @"
                    SELECT ProdottoID, Quantita, Prezzo 
                    FROM Carrello INNER JOIN Prodotti ON Carrello.ProdottoID = Prodotti.ProdottoID
                    WHERE UtenteID = @UtenteID";

                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@UtenteID", utenteId);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            cartItems.Add(new CartItem
                            {
                                ProdottoID = reader.GetInt32(0),
                                Quantita = reader.GetInt32(1),
                                Prezzo = reader.GetDecimal(2)
                            });

                            totale += reader.GetDecimal(2) * reader.GetInt32(1);
                        }
                    }
                }

                if (cartItems.Count == 0)
                    return RedirectToAction("Index", new { utenteId });

                string insertOrderQuery = @"
                    INSERT INTO Ordini (UtenteID, IndirizzoSpedizione, DataOrdine, TotaleOrdiniPrezzo, StatoOrdine, StatoPagamento)
                    VALUES (@UtenteID, 'Indirizzo di test', GETDATE(), @Totale, 'In lavorazione', 'In attesa');
                    SELECT SCOPE_IDENTITY();";

                int ordineId;
                using (SqlCommand command = new SqlCommand(insertOrderQuery, connection))
                {
                    command.Parameters.AddWithValue("@UtenteID", utenteId);
                    command.Parameters.AddWithValue("@Totale", totale);
                    ordineId = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                foreach (var item in cartItems)
                {
                    string insertDetailQuery = @"
                        INSERT INTO OrdiniDettagli (OrdineID, ProdottoID, Quantita, PrezzoUnitario)
                        VALUES (@OrdineID, @ProdottoID, @Quantita, @Prezzo)";

                    using (SqlCommand command = new SqlCommand(insertDetailQuery, connection))
                    {
                        command.Parameters.AddWithValue("@OrdineID", ordineId);
                        command.Parameters.AddWithValue("@ProdottoID", item.ProdottoID);
                        command.Parameters.AddWithValue("@Quantita", item.Quantita);
                        command.Parameters.AddWithValue("@Prezzo", item.Prezzo);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                await SvuotaCarrello((int)utenteId);
            }

            return RedirectToAction("Index", "Ordini", new { utenteId });
        }

        // Metodo di supporto per assicurare che l'utente esista
        private async Task<int> EnsureUserExists()
        {
            int utenteId = 1; // ID statico per test

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string checkUserQuery = "SELECT COUNT(*) FROM Utenti WHERE UtenteID = @UtenteID";
                using (SqlCommand command = new SqlCommand(checkUserQuery, connection))
                {
                    command.Parameters.AddWithValue("@UtenteID", utenteId);
                    int count = (int)await command.ExecuteScalarAsync();

                    if (count == 0)
                    {
                        string insertUserQuery = "INSERT INTO Utenti (UtenteID, Nome) VALUES (@UtenteID, 'Mario')";
                        using (SqlCommand insertCommand = new SqlCommand(insertUserQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@UtenteID", utenteId);
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
            }

            return utenteId;
        }
    }
}
