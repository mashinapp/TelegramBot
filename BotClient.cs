using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TelegramBot.Constant;
using TelegramBot.Model;


namespace TelegramBot.Client;

public class BotClient
{
    private HttpClient _client;
    private static string _address;
    private static string _apikey;

    public BotClient()
    {
        _address = Constants.adress;
        _apikey = Constants.apikey;
        _client = new HttpClient();
        _client.BaseAddress = new Uri(_address);
    }

    public async Task<JobSearch?> GetJobByName(string jobName, long userId)
    {
        var responce = await _client.GetAsync($"JobSearch?search={jobName}&user={userId}");
        var content = responce.Content.ReadAsStringAsync().Result;
        var result = JsonConvert.DeserializeObject<JobSearch>(content);
        return result;
    }

    public async Task<JobsResult?> GetJobByPosition(int position, long userId)
    {
        var responce = await _client.GetAsync($"JobSearch/{position}?user={userId}");
        var content = responce.Content.ReadAsStringAsync().Result;
        var result = JsonConvert.DeserializeObject<JobsResult>(content);
        return result;
    }

    public async Task<JobSearch?> PostJob(JobsResult job, long userId)
    {
        var stringContent = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");
        var responce = await _client.PostAsync($"JobSearch?user={userId}", stringContent);
        var content = responce.Content.ReadAsStringAsync().Result;
        var result = JsonConvert.DeserializeObject<JobSearch>(content);
        return result;
    }

    public async Task PostEditJob(int position, JobsResult job, long userId)
    {
        var stringContent = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");
        await _client.PostAsync($"JobSearch/{position}?user={userId}", stringContent);
    }

    public async Task DeleteJob(int position, long userId)
    {
        await _client.DeleteAsync($"JobSearch/{position}?user={userId}");
    }
}

