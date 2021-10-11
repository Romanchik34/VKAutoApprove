using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using VKAutoApprove;

class Program
{
    private static Thread MainThread;

    private static string Token;
    public static int GroupID;
    public static int Delay;

    // В будущем VK может не поддерживать дефолтную версию API, так что можете изменить её
    private static string Version = "5.131";

    private static string GetSettings(string key)
    {
        StringBuilder builder = new StringBuilder(255);
        IniWorker.GetPrivateProfileString("Settings", key, "", builder, 255, "./Settings.cfg");
        return builder.ToString();
    }

    static void Main(string[] args)
    {
        if (!File.Exists("Settings.cfg"))
        {
            IniWorker.WritePrivateProfileString("Settings", "Token", "", "./Settings.cfg");
            IniWorker.WritePrivateProfileString("Settings", "GroupID", "", "./Settings.cfg");
            IniWorker.WritePrivateProfileString("Settings", "Interval", "", "./Settings.cfg");

            Console.WriteLine("Не найден файл Settings.cfg");
            Console.WriteLine("Он был только что сгенерирован для примера. Вам осталось внести в него данные");
            Console.WriteLine("После настройки перезапустите программу");
            Console.ReadLine();
            return;
        }

        string token = GetSettings("Token");
        if ((Token = token) == "")
        {
            Console.WriteLine("В настройках не указан токен VK. Укажите и перезапустите программу");
            Console.ReadLine();
            return;
        }

        string groupID = GetSettings("GroupID");
        if (groupID == "")
        {
            Console.WriteLine("В настройках не указан GroupID. Укажите и перезапустите программу");
            Console.ReadLine();
            return;
        }
        if (!int.TryParse(groupID, out GroupID))
        {
            Console.WriteLine("В настройках не указан нечисловой GroupID. Исправьте и перезапустите программу");
            Console.ReadLine();
            return;
        }

        string delay = GetSettings("Interval");
        if (delay == "")
        {
            Console.WriteLine("В настройках не указан Interval. Исправьте и перезапустите программу");
            Console.ReadLine();
            return;
        }
        if (!int.TryParse(delay, out Delay))
        {
            Console.WriteLine("В настройках не указан нечисловой Interval. Исправьте и перезапустите программу");
            Console.ReadLine();
            return;
        }

        Console.OutputEncoding = Encoding.Unicode;
        Console.CursorVisible = false;
        Console.Title = $"Автопринятие заявок | Статус: Подготовка к запуску | ID Группы: {GroupID} | Интервал: {Delay}";

        Log($"Начат процесс принятия заявок раз в {Delay} секунд");
        Log("Чтобы остановить процесс, нажмите Enter");
        Console.WriteLine();

        MainThread = new Thread(DoWork);
        MainThread.Start();

        Console.ReadLine(); // Enter
        MainThread.Abort();

        Console.WriteLine("Процесс сбора заявок остановлен");
        Console.WriteLine("Нажмите Enter ещё раз для завершения программы");
        Console.ReadLine();
    }

    async static void DoWork()
    {
        Console.Title = $"Автопринятие заявок | Статус: Работает | ID Группы: {GroupID} | Интервал: {Delay}";

        while (true)
        {
            try
            {
                Log("Начинаю процесс сбора заявок...");
                List<int> requests = await GetGroupRequests();

                Log($"Список заявок в группу (всего: {requests.Count})");
                foreach (var request in requests)
                {
                    Log("ID: " + request);
                }

                if (requests.Count > 0)
                {
                    foreach (var userID in requests)
                    {
                        Log("Принимаю заявку от: " + userID);

                        if (await ApproveRequest(userID))
                            Log($"Заявка от {userID} успешно принята");
                    }
                }

                Log($"Все заявки были обработаны. Следующая проверка через {Delay} секунд");
            }
            catch (Exception ex)
            {
                Log("Произошла ошибка в ходе работы: " + ex);
            }
            finally
            {
                Thread.Sleep(Delay * 1000);
                Console.WriteLine();
            }
        }
    }

    private static void Log(string text = "")
    {
        Console.WriteLine($"{DateTime.Now.ToString("g")} | {text}");
    }

    async static Task<List<int>> GetGroupRequests()
    {
        return await Task.Run(async () =>
        {
            HttpClient client = new HttpClient();

            var response = await client.GetAsync($"https://api.vk.com/method/groups.getRequests?group_id={GroupID}&access_token={Token}&v={Version}");
            var responseStr = await response.Content.ReadAsStringAsync();
            client.Dispose();

            var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseStr);
            if (jsonResponse.ContainsKey("error"))
            {
                Console.WriteLine("Error: " + jsonResponse["error"]);
                Console.ReadLine();
                return new List<int>();
            }

            var vkResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse["response"].ToString());
            var requests = JsonConvert.DeserializeObject<List<int>>(vkResponse["items"].ToString());
            return requests;
        });
    }

    async static Task<bool> ApproveRequest(int userID)
    {
        return await Task.Run(async () =>
        {
            HttpClient client = new HttpClient();

            var response = await client.GetAsync($"https://api.vk.com/method/groups.approveRequest?group_id={GroupID}&user_id={userID}&access_token={Token}&v={Version}");
            var responseStr = await response.Content.ReadAsStringAsync();
            client.Dispose();

            var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseStr);
            if (jsonResponse.ContainsKey("error"))
            {
                Console.WriteLine("Error: " + jsonResponse["error"]);
                Console.ReadLine();
                return false;
            }

            return true;
        });
    }
}