using Xunit;
using Watchly.Web.Models.ViewModels;
using Watchly.Web.Validators;

namespace Watchly.Tests;

public class ValidationTests
{
    [Fact]
    public void RegisterValidator_ValidModel_Passes()
    {
        var validator = new RegisterViewModelValidator();
        var result = validator.Validate(new RegisterViewModel
        {
            Email = "user@test.com",
            Username = "user_1",
            Password = "Password1",
            ConfirmPassword = "Password1"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RegisterValidator_InvalidEmail_Fails()
    {
        var validator = new RegisterViewModelValidator();
        var result = validator.Validate(new RegisterViewModel { Email = "bad", Username = "user_1", Password = "Password1", ConfirmPassword = "Password1" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RegisterValidator_PasswordMismatch_Fails()
    {
        var validator = new RegisterViewModelValidator();
        var result = validator.Validate(new RegisterViewModel { Email = "user@test.com", Username = "user_1", Password = "Password1", ConfirmPassword = "Password2" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LoginValidator_ValidModel_Passes()
    {
        var validator = new LoginViewModelValidator();
        var result = validator.Validate(new LoginViewModel { EmailOrUsername = "user", Password = "123" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LoginValidator_EmptyLogin_Fails()
    {
        var validator = new LoginViewModelValidator();
        var result = validator.Validate(new LoginViewModel { EmailOrUsername = "", Password = "123" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LoginValidator_EmptyPassword_Fails()
    {
        var validator = new LoginViewModelValidator();
        var result = validator.Validate(new LoginViewModel { EmailOrUsername = "user", Password = "" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void MovieValidator_ValidModel_Passes()
    {
        var validator = new MovieCreateEditViewModelValidator();
        var result = validator.Validate(new MovieCreateEditViewModel { Title = "Movie", ReleaseYear = 2020, Rating = 8.5m });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MovieValidator_InvalidRating_Fails()
    {
        var validator = new MovieCreateEditViewModelValidator();
        var result = validator.Validate(new MovieCreateEditViewModel { Title = "Movie", ReleaseYear = 2020, Rating = 12m });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void MovieValidator_InvalidYear_Fails()
    {
        var validator = new MovieCreateEditViewModelValidator();
        var result = validator.Validate(new MovieCreateEditViewModel { Title = "Movie", ReleaseYear = 1800, Rating = 8m });
        Assert.False(result.IsValid);
    }
    [Fact]
    public void MovieValidator_RelativeVideoUrl_Passes()
    {
        var validator = new MovieCreateEditViewModelValidator();
        var result = validator.Validate(new MovieCreateEditViewModel { Title = "Movie", ReleaseYear = 2020, Rating = 8m, VideoUrl = "/videos/vid1.mp4" });
        Assert.True(result.IsValid);
    }


    [Fact]
    public void MovieValidator_WwwrootVideoPath_Passes()
    {
        var validator = new MovieCreateEditViewModelValidator();
        var result = validator.Validate(new MovieCreateEditViewModel { Title = "Movie", ReleaseYear = 2020, Rating = 8m, VideoUrl = "src/Watchly.Web/wwwroot/videos/vid1.mp4" });
        Assert.True(result.IsValid);
    }


}
