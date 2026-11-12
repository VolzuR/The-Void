public class Package : Item
{
    public string destinationId = "House_A";
    public int reward = 50;

    public override string Title => "Посылка → " + destinationId;

    public override string Details
    {
        get
        {
            if (!string.IsNullOrEmpty(description)) return description;
            return "Посылка весом примерно " + weight.ToString("0.#") + " кг. Нужно доставить по адресу "
                   + destinationId + ". Награда: " + reward + ".";
        }
    }
}