using AdPlatformLocator.Models;
using System.Collections.Concurrent;

namespace AdPlatformLocator.Services
{
    public class AdPlatformService
    {
        /// <summary>
        /// Хранилище данных о рекламных площадках
        /// </summary>
        private ConcurrentDictionary<string, HashSet<string>> _locationAdvertisers = new();

        /// <summary>
        /// Метод для загрузки данных рекламных площадок из текста
        /// </summary>
        /// <param name="adPlatformsTextData">Текстовые данные о рекламных площадках</param>
        /// <returns>Число обработанных строк и число загрузженых локаций</returns>
        public (int LinesCount, int LocationsCount) LoadData(string adPlatformsTextData)
        {
            // Очищаем старые данные перед загрузкой новых, так как загрузка должна полностью перезаписывать информацию.
            _locationAdvertisers.Clear();

            // Разделяем содержимое файла на строки
            var lines = adPlatformsTextData.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(':', 2); // Разделяем по первому двоеточию на две части

                if (parts.Length != 2)
                    continue;

                var adPlacementName = parts[0].Trim();
                var locationsString = parts[1].Trim();

                // Разделяем локации по запятой
                var locations = locationsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(l => l.Trim())
                                                .ToList();

                foreach (var location in locations)
                {
                    // Добавляем рекламную площадку в HashSet для каждой локации.
                    // Если локации еще нет в словаре, создаем новый HashSet.
                    _locationAdvertisers.GetOrAdd(location, new HashSet<string>()).Add(adPlacementName);
                }
            }
            return (lines.Count(), _locationAdvertisers.Count);
        }

        /// <summary>
        /// Метод для поиска рекламных площадок по заданной локации
        /// </summary>
        /// <param name="location">Локация реклалных площадок</param>
        /// <returns>
        /// Список рекламных площадок по локации
        /// </returns>
        public HashSet<string> SearchAdPlatforms(string location)
        {
            var foundAdPlacements = new HashSet<string>();
            var parentLocations = GetParentLocations(location);

            foreach (var loc in parentLocations)
            {
                // Пытаемся получить список рекламных площадок для каждой родительской локации
                if (_locationAdvertisers.TryGetValue(loc, out var adPlacements))
                {
                    foreach (var adPlacement in adPlacements)
                    {
                        foundAdPlacements.Add(adPlacement);
                    }
                }
            }

            return foundAdPlacements;
        }

        /// <summary>
        /// Вспомогательный метод для получения всех родительских локаций
        /// </summary>
        /// <param name="location">Локация рекламной площадки</param>
        /// <remarks>
        /// Если локация была только корневой (например, "/ru"), то цикл не добавит ее родителя. Например, для "/ru" GetParentLocations вернет["/ru"].
        /// </remarks>
        /// <returns>
        /// Список всех родительских локаций
        /// Например, для "/ru/svrd/revda" это будут "/ru/svrd/revda", "/ru/svrd", "/ru".
        /// </returns>
        private static List<string> GetParentLocations(string location)
        {
            var parents = new List<string>();
            var currentPath = location;

            // Добавляем саму локацию
            parents.Add(currentPath);

            // Итерируемся, пока не достигнем корневой локации (например, "/ru")
            while (currentPath.LastIndexOf('/') > 0)
            {
                // Отсекаем последний сегмент пути
                currentPath = currentPath.Substring(0, currentPath.LastIndexOf('/'));
                parents.Add(currentPath);
            }

            return parents;
        }
    }
}