using AdPlatformLocator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AdPlatformLocator.Controllers
{
    [Route("api")]
    [ApiController]
    public class AdPlatformController : ControllerBase
    {
        private readonly AdPlatformService _adPlatformService;
        public AdPlatformController(AdPlatformService adPlatformService)
        {
            _adPlatformService = adPlatformService;
        }
        /// <summary>
        /// Загружает данные о рекламных площадках из текстового файла.
        /// Полностью перезаписывает всю хранимую информацию.
        /// </summary>
        /// <param name="file">Данные загрузженного файла.</param>
        /// <returns>Статус Ok в случае успешной загрузки.</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран.");

            using var reader = new StreamReader(file.OpenReadStream());
            string fileContent = await reader.ReadToEndAsync();

            try
            {
                var result = _adPlatformService.LoadData(fileContent);
                return Ok($"Обработано {result.LinesCount} строк. Загрузжено {result.LocationsCount} локаций.");
            }
            catch (Exception ex)
            {
                // Логирование ошибки в реальном приложении
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Ищет список рекламных площадок для заданной локации.
        /// </summary>
        /// <param name="location">Локация для поиска.</param>
        /// <returns>Список названий рекламных площадок в формате JSON.</returns>
        [HttpGet("search")]
        public IActionResult Search([FromQuery] string location)
        {
            try
            {
                var adPlacements = _adPlatformService.SearchAdPlatforms(location);

                // Возвращаем список рекламных площадок в формате JSON
                return Ok(adPlacements.ToList()); // Преобразуем HashSet в List для сериализации в JSON
            }
            catch (Exception ex)
            {
                // Логирование ошибки в реальном приложении
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}
