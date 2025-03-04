namespace EcommerceTeam5.Models
{
    public class Carrello
    {
       
            public int CarrelloID { get; set; }
            public int UtenteID { get; set; }
            public List<Product> Products { get; set; }
            public decimal TotaleCarrello { get; set; }

            public Carrello()
            {
                Products = new List<Product>();
            }
        
    }
}
