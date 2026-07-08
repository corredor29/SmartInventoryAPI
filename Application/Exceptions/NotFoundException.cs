namespace Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }

        public NotFoundException(string entityName, object key)
            : base($"{entityName} con id '{key}' no fue encontrado.") { }
    }
}