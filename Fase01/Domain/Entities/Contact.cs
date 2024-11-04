namespace Domain.Entities
{
    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string RegionCode { get; set; }

        public Contact(string name, string phone, string email, string regionCode)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required");
            if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required");
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required");
            if (string.IsNullOrWhiteSpace(regionCode)) throw new ArgumentException("RegionCode is required");

            Name = name;
            Phone = phone;
            Email = email;
            RegionCode = regionCode;
        }
    }
}
