﻿namespace EcommerceTeam5.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string? Nome { get; set; }

        public decimal Prezzo { get; set; }

        public string? Descrizione { get; set; }

        public string? Immagine { get; set; }

        public int Categoria { get; set; }  

        public string? CategoriaNome { get; set; }
        
        public DateTime Creazione { get; set; }
    }
}
