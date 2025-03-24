namespace Application.DTOs
{
    public class ContactDTO
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string RegionCode { get; set; }
    }

    public class Envelope
    {
        public string Method { get; set; }
        public string Route { get; set; }
        public ContactDTO? Message { get; set; }
    }
}
