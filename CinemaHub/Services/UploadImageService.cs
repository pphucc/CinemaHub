namespace CinemaHub.Services
{
	public class UploadImageService
	{
		private readonly IWebHostEnvironment _webHostEnvironment;
        public UploadImageService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }
        public string UploadImage(IFormFile file, string directory, string? oldImageUrl = "")
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string fileName = Guid.NewGuid().ToString();

            var uploadDirectory = Path.Combine(wwwRootPath, directory);

            var extension = Path.GetExtension(file.FileName);

            if (!string.IsNullOrEmpty(oldImageUrl))
            {
				var oldImagePath = Path.Combine(wwwRootPath, oldImageUrl.TrimStart('\\'));
				if (System.IO.File.Exists(oldImagePath))
				{
					System.IO.File.Delete(oldImagePath);
				}
			}
			// Open stream to upload file
			using (var fileStreams = new FileStream(Path.Combine(uploadDirectory, fileName + extension), FileMode.Create))
			{
				file.CopyTo(fileStreams);
			}
			return $@"\{directory}\{fileName}{extension}";
		}
    }
}
