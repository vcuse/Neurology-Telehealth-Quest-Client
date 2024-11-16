using System;

public interface IJSonObject<T>
{
    string ConvertToJSON();
    static T FromJSON(string jsonString) => throw new NotImplementedException();
}
