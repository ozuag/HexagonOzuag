// rengi olmayan hexagonlar da gelebilir (elmas, star vb -> HexaFall'da varlar)
public interface IColorHexagon
{
   //void SetColor(int _colorId);

    int ColorId { get; }

    // kendisi ile aynı renkte 3'lü grup oluşturabildi mi
    int TripletState();

}