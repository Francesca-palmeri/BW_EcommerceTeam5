public class Product
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public decimal Prezzo { get; set; }
    public string Descrizione { get; set; }
    public string Immagine { get; set; }
    public int CategoriaID { get; set; }  // ID della categoria
    public string NomeCategoria { get; set; } // Nome della categoria
    public DateTime Creazione { get; set; }
}


