using BookingSystem.Application.Rooms.CreateRoom;
using BookingSystem.Application.Rooms.UpdateRoomDetails;
using FluentValidation.TestHelper;
using Shouldly;

namespace BookingSystem.UnitTests.Application.Validators;

public sealed class RoomCommandValidatorsTests
{
    private static readonly CreateRoomCommand ValidCreate = new("Court 1", 4, 15m, "USD");
    private static readonly UpdateRoomDetailsCommand ValidUpdate = new(Guid.NewGuid(), "Court 1", 4, 15m, "USD");

    private readonly CreateRoomCommandValidator _createValidator = new();
    private readonly UpdateRoomDetailsCommandValidator _updateValidator = new();

    [Fact]
    public void CreateRoom_WithValidCommand_HasNoErrors()
    {
        _createValidator.TestValidate(ValidCreate).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRoom_WithBlankName_HasNameError(string name)
    {
        _createValidator.TestValidate(ValidCreate with { Name = name })
            .ShouldHaveValidationErrorFor(command => command.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U5D")]
    public void CreateRoom_WithMalformedCurrency_HasSingleCurrencyError(string currency)
    {
        _createValidator.TestValidate(ValidCreate with { Currency = currency })
            .ShouldHaveValidationErrorFor(command => command.Currency)
            .Count().ShouldBe(1);
    }

    [Fact]
    public void CreateRoom_WithLowercaseCurrency_HasNoErrors()
    {
        _createValidator.TestValidate(ValidCreate with { Currency = "eur" }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateRoomDetails_WithValidCommand_HasNoErrors()
    {
        _updateValidator.TestValidate(ValidUpdate).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateRoomDetails_WithEmptyRoomId_HasRoomIdError()
    {
        _updateValidator.TestValidate(ValidUpdate with { RoomId = Guid.Empty })
            .ShouldHaveValidationErrorFor(command => command.RoomId);
    }

    [Fact]
    public void UpdateRoomDetails_WithMalformedCurrency_HasCurrencyError()
    {
        _updateValidator.TestValidate(ValidUpdate with { Currency = "DOLLARS" })
            .ShouldHaveValidationErrorFor(command => command.Currency);
    }
}
