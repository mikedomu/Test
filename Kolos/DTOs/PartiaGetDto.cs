namespace Kolos.DTOs;

public class PartiaGetDto
{
    public int Id { get; set; }
    public string Nazwa { get; set; }
    public string Skrot { get; set; }
    public DateTime DataZalozenia { get; set; }
    public List<PoliticianSimpleDTO> Czlonkowie { get; set; }
}