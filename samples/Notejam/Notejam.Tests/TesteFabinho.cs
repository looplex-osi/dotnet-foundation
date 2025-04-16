using Newtonsoft.Json;

namespace CaseManagement.Tests;

[TestClass]
public class TesteFabinho
{
    [TestMethod]
    public void Teste()
    {

        var list = new List<object>();

        list.Add(new Fabinho());
        list.Add(new Fernando());

        foreach (var item in list)
        {
            if (item is Fabinho fabinho)
            {
                Console.WriteLine(fabinho.Peso);
            }
            else if (item is Fernando fernando)
            {
                Console.WriteLine(fernando.Carteira);
            } 
        }

        string listJson = JsonConvert.SerializeObject(list);
        
        
        
        Assert.IsTrue(true);
    }

    public class Fabinho
    {
        public bool Beautiful = true;
        public bool ShouldSerializeBeautiful() => Peso > 50;
        public int Peso = 5;
    }

    public class Fernando
    {
        public double Carteira = 10000000.0;
        
        [JsonIgnore]
        public bool Nice = false; 
    }
}