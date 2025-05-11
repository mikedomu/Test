using System.ComponentModel.DataAnnotations;

namespace Kolos.DTOs;

public class PoliticianCreateDto
{
    [MaxLength(50)]
    public required string Imie { get; set; }
    [MaxLength(50)]
    public required string Nazwisko { get; set; }
    [MaxLength(150)]
    public required string Powiedzonko { get; set; }
    
    public List<int>? Przynaleznosc { get; set; }
    
    
}