namespace Lalamog.Api.DTOs
{
    public class StoredFile
    {
        public StoredFile(Stream content, string contentType)
        {
            Content = content;
            ContentType = contentType;
        }

        public Stream Content { get; }

        public string ContentType { get; }
    }
}
