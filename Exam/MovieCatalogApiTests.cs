using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using MovieCatalog.Models;



namespace MovieCatalog;


[TestFixture]
public class Tests
{
    private RestClient client;
    private static string lastCreatedMovieId;

    private const string BaseUrl = "http://144.91.123.158:5000";
    private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJlZmUwMjFlOC1mODU1LTRmZWMtOGRjYy1hOWEzMjgwYWViNDIiLCJpYXQiOiIwNC8xOC8yMDI2IDA2OjE3OjUxIiwiVXNlcklkIjoiOTcxNmMyYjQtYjMzNy00OTRmLTYyNDQtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJib3Jpcy5zLm5lbm92QGdtYWlsLmNvbSIsIlVzZXJOYW1lIjoiYm5lbm92IiwiZXhwIjoxNzc2NTE0NjcxLCJpc3MiOiJNb3ZpZUNhdGFsb2dfQXBwX1NvZnRVbmkiLCJhdWQiOiJNb3ZpZUNhdGFsb2dfV2ViQVBJX1NvZnRVbmkifQ.5U-BXKdtBp1lYPDvsVKQK51RUtLi09i_Bs8rofaCGZ4";

    private const string LoginEmail = "boris.s.nenov@gmail.com";
    private const string LoginPassword = "abv123";

    [OneTimeSetUp]
    public void Setup()
    {
        string jwtToken;

        if (!string.IsNullOrWhiteSpace(StaticToken))
        {
            jwtToken = StaticToken;
        }
        else
        {
            jwtToken = GetJwtToken(LoginEmail, LoginPassword);
        }

        var options = new RestClientOptions(BaseUrl)
        {
            Authenticator = new JwtAuthenticator(jwtToken)
        };

        this.client = new RestClient(options);
    }

    private string GetJwtToken(string email, string password)
    {
        var tempClient = new RestClient(BaseUrl);
        var request = new RestRequest("/api/User/Authentication", Method.Post);
        request.AddJsonBody(new { email, password });

        var response = tempClient.Execute(request);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var token = content.GetProperty("token").GetString();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Token not found in the response.");
            }
            return token;
        }
        else
        {
            throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
        }
    }

    [Order(1)]
    [Test]
    public void CreateMovie_WithRequiredFields_ShouldReturnSuccess()
    {
        var movieData = new MovieDTO
        {
            Title = "Terminator",
            Description = "Action movie.",
        };

        var request = new RestRequest("/api/Movie/Create", Method.Post);
        request.AddJsonBody(movieData);

        var response = this.client.Execute(request);

        var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");

        Assert.IsInstanceOf<ApiResponseDTO>(createResponse);
        Assert.That(createResponse, Is.Not.Null);
        Assert.That(createResponse.Movie, Is.Not.Null);
        Assert.IsInstanceOf<MovieDTO>(createResponse.Movie);

        Assert.That(createResponse.Msg, Is.EqualTo("Movie created successfully!"));
        lastCreatedMovieId = createResponse.Movie.Id;
        Assert.That(lastCreatedMovieId, Is.Not.Null.And.Not.Empty);
        Assert.IsNotEmpty(createResponse.Movie.Id);
    }

    [Order(2)]
    [Test]
    public void EditExistingMovieShouldReturnSuccess()
    {
        var editRequestData = new MovieDTO
        {
            Title = "Edited Movie Terminator",
            Description = "This is a edited movie description.",
           
        };


        var request = new RestRequest("/api/Movie/Edit", Method.Put);

        request.AddQueryParameter("movieId", lastCreatedMovieId);
        request.AddJsonBody(editRequestData);

        var response = this.client.Execute(request);

        var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
        Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));
    }

    [Order(3)]
    [Test]
    public void GetAllMoviesShouldReturnSuccess()
    {
        var request = new RestRequest("/api/Catalog/All", Method.Get);
        var response = this.client.Execute(request);

        var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
        Assert.That(responseItems, Is.Not.Empty);
        Assert.That(responseItems, Is.Not.Null);

    }

    [Order(4)]
    [Test]

    public void DeleteMovieShouldReturnSuccess()
    {
        var request = new RestRequest("/api/Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", lastCreatedMovieId);
        var response = this.client.Execute(request);

        var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

        Assert.That(deleteResponse, Is.Not.Null);
        Assert.That(deleteResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
    }

    [Order(5)]
    [Test]
    public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest()
    {
        var movieData = new MovieDTO
        {
            Title = "",
            Description = "",
        };

        var request = new RestRequest("/api/Movie/Create", Method.Post);
        request.AddJsonBody(movieData);

        var response = this.client.Execute(request);

        var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "BadRequest (400).");

      
    }

    [Order(6)]
    [Test]
    public void EditNonExistingMovieShouldReturnBadRequest()
    {
        string nonExistingMovieId = "5674534";
        var editRequestData = new MovieDTO
        {
            Title = "Edited movie",
            Description = "Edited movie description.", 
        };

        var request = new RestRequest("/api/Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", nonExistingMovieId);
        request.AddJsonBody(editRequestData);

        var response = this.client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "BadRequest (400).");
    }

    [Order(7)]
    [Test]

    public void DeleteNonExistingMovie_ShouldReturnNotFound()
    {
        string nonExistingMovieId = "5674534";

        var request = new RestRequest("/api/Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", nonExistingMovieId);
        var response = this.client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "BadRequest (400).");

        var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

        Assert.That(deleteResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client?.Dispose();
    }
}