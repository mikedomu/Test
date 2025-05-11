namespace Kolos.DTOs;

public class PoliticianGetDto
{
    public int Id { get; set; }
    public string Imie { get; set; }
    public string Nazwisko { get; set; }
    public string? Powiedzonko { get; set; }
    public List<PrzynaleznoscGetDto> Przynaleznosc { get; set; }
    
}