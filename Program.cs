using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YTDownloader
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            string filepath = args[0];
            string[] lines = File.ReadAllLines(filepath);
            string appdata_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YTDownloader");

            // create path in appdata if not exists
            if (!Path.Exists(appdata_path))
            {
                Directory.CreateDirectory(appdata_path);
            }

            List<string> names = [.. lines];

            YoutubeClient youtube = new();
            await Download(youtube, names, appdata_path);

            Console.Out.WriteLineAsync("Done");
        }

        static async Task Download(YoutubeClient client, List<string> names, string output)
        {
            List<Task> downloadTasks = new();

            // loop through all videos
            foreach (string s in names)
            {
                
                // check if file already downloaded
                string final_path = Path.Combine(output, s + ".mp4");
                if (File.Exists(final_path))
                {
                    Console.WriteLine($"{s} already downloaded, next...");
                    continue;
                }
                
                try
                {
                    var searchResults = await client.Search.GetVideosAsync($"{s}");
                    VideoSearchResult? firstVideo = searchResults.FirstOrDefault();

                    if (firstVideo != null)
                    {
                        Video video = await client.Videos.GetAsync(firstVideo.Id);
                        StreamManifest streamInfoSet = await client.Videos.Streams.GetManifestAsync(video.Id);

                        // Downloading the video
                        IVideoStreamInfo videoStreamInfo = streamInfoSet.GetMuxedStreams().GetWithHighestVideoQuality();

                        if (videoStreamInfo != null)
                        {
                            Stream videoStream = await client.Videos.Streams.GetAsync(videoStreamInfo);
                            downloadTasks.Add(videoStream.CopyToAsync(File.OpenWrite(final_path)));
                            Console.WriteLine($"Downloaded video: {final_path}");
                        }

                        else
                        {
                            throw new Exception($"No Video stream found for {s}");
                        }
                    }

                    else
                    {
                        throw new Exception("No Video Found!");
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            await Task.WhenAll(downloadTasks);
        }
    }
}

