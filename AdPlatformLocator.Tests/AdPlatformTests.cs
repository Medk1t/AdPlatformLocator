using AdPlatformLocator.Services;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AdPlatformServiceTests
{
    // Тестовые данные для использования в тестах
    private readonly string _testFileContent = @"
Яндекс.Директ: /ru
Ревдинский рабочий:/ru/svrd/revda,/ru/svrd/pervik
Газета уральских москвичей: /ru/msk,/ru/permobl,/ru/chelobl
Крутая реклама: /ru/svrd
Тестовая площадка:/test/location1,/test/location2
Некорректная строка без двоеточия
Площадка без локаций:
Площадка с пустыми локациями: , ,
";

    [Fact] // Атрибут, помечающий метод как одиночный тестовый случай
    public void LoadData_ShouldParseAndStoreCorrectly()
    {
        // Arrange (Подготовка): Создаем экземпляр сервиса
        var service = new AdPlatformService();

        // Act (Действие): Загружаем тестовые данные
        service.LoadData(_testFileContent);

        // Assert (Проверка): Проверяем, что данные загружены корректно
        // Проверяем, что Яндекс.Директ присутствует для /ru
        var ruPlacements = service.SearchAdPlatforms("/ru");
        Assert.Contains("Яндекс.Директ", ruPlacements);

        // Проверяем, что Ревдинский рабочий присутствует для /ru/svrd/revda
        var revdaPlacements = service.SearchAdPlatforms("/ru/svrd/revda");
        Assert.Contains("Ревдинский рабочий", revdaPlacements);
        Assert.Contains("Яндекс.Директ", revdaPlacements); // Должен быть и Яндекс.Директ через родительскую /ru
        Assert.Contains("Крутая реклама", revdaPlacements); // Должна быть и Крутая реклама через родительскую /ru/svrd

        // Проверяем, что 'Тестовая площадка' корректно добавлена
        var testLoc1Placements = service.SearchAdPlatforms("/test/location1");
        Assert.Contains("Тестовая площадка", testLoc1Placements);
        var testLoc2Placements = service.SearchAdPlatforms("/test/location2");
        Assert.Contains("Тестовая площадка", testLoc2Placements);

        // Проверяем, что пустые строки или строки с некорректным форматом не вызывают ошибок
        // и не добавляют некорректные данные
        // Примечание: SearchAdPlatforms для пустой строки или строки из пробелов возвращает пустой HashSet
        var emptyLocPlacements = service.SearchAdPlatforms("");
        Assert.Empty(emptyLocPlacements);
    }

    [Fact]
    public void LoadData_ShouldClearPreviousData()
    {
        // Arrange
        var service = new AdPlatformService();
        service.LoadData("Old Ad: /old/location"); // Загружаем старые данные

        // Act
        service.LoadData(_testFileContent); // Загружаем новые данные (они должны перезаписать старые)

        // Assert
        var oldLocPlacements = service.SearchAdPlatforms("/old/location");
        Assert.DoesNotContain("Old Ad", oldLocPlacements); // Старых данных не должно быть
        Assert.Contains("Яндекс.Директ", service.SearchAdPlatforms("/ru")); // Новые данные должны быть
    }

    [Theory] // Атрибут для параметризованных тестов
    [InlineData("/ru", new string[] { "Яндекс.Директ" })]
    [InlineData("/ru/svrd", new string[] { "Крутая реклама", "Яндекс.Директ" })]
    [InlineData("/ru/svrd/revda", new string[] { "Ревдинский рабочий", "Крутая реклама", "Яндекс.Директ" })]
    [InlineData("/ru/msk", new string[] { "Газета уральских москвичей", "Яндекс.Директ" })]
    [InlineData("/ru/permobl", new string[] { "Газета уральских москвичей", "Яндекс.Директ" })]
    [InlineData("/nonexistent/location", new string[] { })] // Несуществующая локация
    public void SearchAdPlatforms_ShouldReturnCorrectPlacements(string location, string[] expectedPlacementsArray)
    {
        // Arrange
        var service = new AdPlatformService();
        service.LoadData(_testFileContent); // Загружаем тестовые данные

        // Act
        var foundPlacements = service.SearchAdPlatforms(location);

        // Assert
        var expectedPlacements = new HashSet<string>(expectedPlacementsArray); // Преобразуем массив в HashSet для сравнения
        Assert.Equal(expectedPlacements.Count, foundPlacements.Count); // Проверяем количество найденных площадок
        foreach (var expected in expectedPlacements)
        {
            Assert.Contains(expected, foundPlacements); // Проверяем, что каждая ожидаемая площадка присутствует
        }
    }

    [Fact]
    public void SearchAdPlatforms_ShouldHandleEmptyOrNullLocation()
    {
        // Arrange
        var service = new AdPlatformService();
        service.LoadData(_testFileContent);

        // Act & Assert
        var resultNull = service.SearchAdPlatforms(null);
        Assert.Empty(resultNull); // Ожидаем пустой результат для null локации

        var resultEmpty = service.SearchAdPlatforms("");
        Assert.Empty(resultEmpty); // Ожидаем пустой результат для пустой локации

        var resultWhitespace = service.SearchAdPlatforms("   ");
        Assert.Empty(resultWhitespace); // Ожидаем пустой результат для локации из пробелов
    }

    // Тест для GetParentLocations - требует, чтобы GetParentLocations был public
    // В AdPlatformService.cs сделайте метод 'GetParentLocations' публичным для тестирования:
    // public List<string> GetParentLocations(string location)
    [Fact]
    public void GetParentLocations_ShouldReturnCorrectParents()
    {
        // Arrange
        var service = new AdPlatformService(); // Создаем экземпляр сервиса для доступа к методу

        // Act
        var parentsForRevda = AdPlatformService.GetParentLocations("/ru/svrd/revda");
        var parentsForRu = AdPlatformService.GetParentLocations("/ru");
        var parentsForRoot = AdPlatformService.GetParentLocations("/");
        var parentsForEmpty = AdPlatformService.GetParentLocations("");
        var parentsForNull = AdPlatformService.GetParentLocations(null);

        // Assert for /ru/svrd/revda
        Assert.Equal(3, parentsForRevda.Count);
        Assert.Contains("/ru/svrd/revda", parentsForRevda);
        Assert.Contains("/ru/svrd", parentsForRevda);
        Assert.Contains("/ru", parentsForRevda);

        // Assert for /ru
        Assert.Single(parentsForRu);
        Assert.Contains("/ru", parentsForRu);

        // Assert for / (root)
        Assert.Single(parentsForRoot);
        Assert.Contains("/", parentsForRoot);

        // Assert for empty and null
        Assert.Empty(parentsForEmpty);
        Assert.Empty(parentsForNull);
    }
}