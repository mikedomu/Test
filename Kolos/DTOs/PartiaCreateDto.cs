namespace Kolos.DTOs;

public class PartiaCreateDto
{
    public required string Nazwa { get; set; }
    public required string Skrot  {get; set;}
    public required DateTime DataZalozenia { get; set; }
    public required List<int>? Czlonkowie  { get; set; }
    
         
}