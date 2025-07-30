using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

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
            var newMap = new ConcurrentDictionary<string, HashSet<string>>();

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
                    newMap.GetOrAdd(location, new HashSet<string>()).Add(adPlacementName);
                }
            }

            foreach (var item in newMap)
            {
                var foundAdPlacements = new HashSet<string>();
                var pLoc = GetParentLocations(item.Key);
                foreach (var loc in pLoc)
                {
                    if (newMap.TryGetValue(loc, out var adPlacements))
                    {

                        foreach (var adPlacement in adPlacements)
                        {
                            foundAdPlacements.Add(adPlacement);
                        }

                    }
                }
                newMap[item.Key] = foundAdPlacements;
            }
            //При успешной загрузке заменяем ссылку старого списка на новый
            Interlocked.Exchange(ref _locationAdvertisers, newMap);

            return (lines.Count(), newMap.Count);
        }

        /// <summary>
        /// Метод для поиска рекламных площадок по заданной локации
        /// </summary>
        /// <param name="location">Локация реклалных площадок</param>
        /// <returns>
        /// Список рекламных площадок по локации
        /// </returns>
        public HashSet<string> SearchAdPlatforms(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return new HashSet<string>();

            if (_locationAdvertisers.TryGetValue(location, out var adPlacements))
                return adPlacements;

            return new HashSet<string>();
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
        public static List<string> GetParentLocations(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return new List<string>();

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