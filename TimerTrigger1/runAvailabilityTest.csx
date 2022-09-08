using System.Net.Http; 
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.Globalization;

public async static Task RunAvailabilityTestAsync(ILogger log) { 
	log.LogInformation($"C# Timer trigger function started executing at: {DateTime.Now}");
	using (var httpClient = new HttpClient()) { 
 		httpClient.DefaultRequestHeaders.Add($"Authorization", $"Basic  {GetEnvironmentVariable("BASIC_AUTH")}");
   		string logHttpOutputResult = await httpClient.GetStringAsync("https://gilbertrocks.scm.azurewebsites.net/vfs/data/jobs/continuous/letsencrypt(letsencrypt.siteextension.job)/job_log.txt"); 
		
		string pattern1 = @"(\d{1,2}/\d{1,2}/\d{4,4} \d{2}:\d{2}:\d{2} >) (.*)(: ERR ](.*))";
		string pattern2 = @"\d{1,2}/\d{1,2}/\d{4,4} \d{2}:\d{2}:\d{2}";
		DateTime currTime = DateTime.Now;
		TimeSpan timeToSubtract = TimeSpan.FromMinutes(int.Parse(GetEnvironmentVariable("LOG_OUTPUT_FREQUENCY")));
		DateTime finalCurrentTime = currTime.Subtract(timeToSubtract);
		int numOfErrors = 0;
		
        foreach (Match errorLog in Regex.Matches(logHttpOutputResult, pattern1,RegexOptions.IgnoreCase)) {
                Match matchedValidDate = Regex.Match(errorLog.Value, pattern2,RegexOptions.IgnoreCase); // match log entries with valid date format (i.e 09/23/2019 11:23:50) 
				if(matchedValidDate.Success){
					DateTime validLogDateTime = DateTime.Parse(matchedValidDate.Value);
				    int res = DateTime.Compare(finalCurrentTime, validLogDateTime);  // returns <0 since param 1 is earlier than param2
					if(res < 0){
						numOfErrors = numOfErrors + 1;
						log.LogError("'{0}' ", errorLog.Value);
					}
					if(numOfErrors >= int.Parse(GetEnvironmentVariable("NUMBER_OF_ERRORS"))){
						throw new Exception("Found some error in the past 5 mins");
					}
				}
        }
		log.LogInformation($"C# Timer trigger function finished execution and processing at: {DateTime.Now}");
	}
}
	
	
public static string GetEnvironmentVariable(string name){
	return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}