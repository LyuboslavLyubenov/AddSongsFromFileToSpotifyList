using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace create_playlist_from_youtube
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientId = args[0];
            var clientSecret = args[1];
            var playlistId = args[2];
            var videosNames = File.ReadAllLines(args[3]);
            var auth = new AuthorizationCodeAuth(clientId, clientSecret, "http://localhost:4002", "http://localhost:4002", Scope.PlaylistModifyPublic | Scope.PlaylistReadPrivate | Scope.PlaylistModifyPrivate);
            
            auth.AuthReceived += async (sender, payload) =>
            {
                auth.Stop();
                
                var token = await auth.ExchangeCode(payload.Code);
                var api = new SpotifyWebAPI()
                {
                    TokenType = token.TokenType,
                    AccessToken = token.AccessToken
                };

                var playlist = api.GetPlaylist(playlistId);
                
                if (playlist.HasError())
                {
                    Console.WriteLine(playlist.Error.Message);
                    return;
                }

                Console.WriteLine();
                
                foreach (var videoName in videosNames)
                {
                
                    var songName = videoName.ToLower().Replace("-", "");
                    songName = Regex.Replace(songName, @"[(\[][A-Za-z0-9 ]+[)\]]", "");
                    Console.WriteLine("Searching for " + videoName);

                    var cuttedVideoName = songName.Substring(0, Math.Min(35, songName.Length));
                    var searchResult = api.SearchItems(cuttedVideoName, SpotifyAPI.Web.Enums.SearchType.Track, 1);
                    if (searchResult.HasError() || searchResult.Tracks == null || searchResult.Tracks.Items == null ||!searchResult.Tracks.Items.Any())
                    {
                        Console.WriteLine("Cant find song matching " + songName);
                        continue;
                    }

                    var track = searchResult.Tracks.Items.First();
                    var trackUri = track.Uri;

                    var addResult = api.AddPlaylistTrack(playlist.Id, trackUri);

                    if (addResult.HasError())
                    {
                        Console.WriteLine("Cant add to playlist.");
                    }
                    else
                    {
                        Console.WriteLine("Adding to playlist");
                    }

                    Console.WriteLine();
                }
            };
            
            auth.Start();
            auth.OpenBrowser();

            Console.ReadLine();
        }
    }
}
