namespace EcommerceTeam5.Models
{
    public class CartItem
    {
        public int CarrelloID { get; set; }
        public int UtenteID { get; set; }
        public int ProdottoID { get; set; }
        public string NomeProdotto { get; set; }
        public string ImmagineProdotto { get; set; }
        public decimal Prezzo { get; set; }
        public int Quantita { get; set; }
        public decimal Totale => Prezzo * Quantita;
    }
}
