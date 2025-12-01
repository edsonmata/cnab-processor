// ========================================
// File: CnabProcessor.UnitTests/CnabFileValidatorTests.cs
// Purpose: Unit tests for CNAB file validator
// ========================================

using CnabProcessor.Api.Validators;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace CnabProcessor.UnitTests;

/// <summary>
/// Unit tests for CnabFileValidator.
/// Tests file validation logic in isolation.
/// </summary>
public class CnabFileValidatorTests
{
    #region Null/Empty File Tests

    [Fact]
    public void Validate_NullFile_ReturnsError()
    {
        // Act
        var result = CnabFileValidator.Validate(null);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("No file was uploaded", result.GetErrorMessage());
    }

    [Fact]
    public void Validate_EmptyFile_ReturnsError()
    {
        // Arrange
        var fileMock = CreateMockFile("test.txt", "", "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("empty", result.GetErrorMessage().ToLower());
    }

    #endregion

    #region File Size Tests

    [Fact]
    public void Validate_FileTooLarge_ReturnsError()
    {
        // Arrange - Create 11 MB file (exceeds 10 MB limit)
        var largeContent = new string('1', 11 * 1024 * 1024);
        var fileMock = CreateMockFile("large.txt", largeContent, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("too large", result.GetErrorMessage().ToLower());
    }

    [Fact]
    public void Validate_ValidFileSize_Passes()
    {
        // Arrange - Create valid CNAB content
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("test.txt", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region File Extension Tests

    [Fact]
    public void Validate_TxtExtension_Passes()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("data.txt", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CnabExtension_Passes()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("data.cnab", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NoExtension_Passes()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("CNAB", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidExtension_ReturnsError()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("data.pdf", content, "application/pdf");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("extension", result.GetErrorMessage().ToLower());
    }

    #endregion

    #region Content Type Tests

    [Fact]
    public void Validate_TextPlainContentType_Passes()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("data.txt", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_OctetStreamContentType_Passes()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("data.txt", content, "application/octet-stream");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region File Name Tests

    [Fact]
    public void Validate_EmptyFileName_ReturnsError()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("name is missing", result.GetErrorMessage().ToLower());
    }

    [Fact]
    public void Validate_TooLongFileName_ReturnsError()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var longFileName = new string('a', 256) + ".txt";
        var fileMock = CreateMockFile(longFileName, content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("too long", result.GetErrorMessage().ToLower());
    }

    #endregion

    #region CNAB Format Tests

    [Fact]
    public void Validate_ValidCnabFormat_Passes()
    {
        // Arrange - Valid CNAB line
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("data.txt", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidFirstCharacter_ReturnsError()
    {
        // Arrange - First character is not a digit
        var content = "A201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var fileMock = CreateMockFile("data.txt", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("transaction type", result.GetErrorMessage().ToLower());
    }

    [Fact]
    public void Validate_NonStandardLineLength_AddsWarning()
    {
        // Arrange - Line is shorter than 81 characters
        var content = "3201903010000014200096206760174753****3153141358JOÃO";
        var fileMock = CreateMockFile("data.txt", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.True(result.IsValid); // Should still pass with warning
        Assert.True(result.Warnings.Count > 0);
    }

    [Fact]
    public void Validate_OnlyWhitespace_ReturnsError()
    {
        // Arrange
        var content = "   \n  \n  ";
        var fileMock = CreateMockFile("data.txt", content, "text/plain");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("empty", result.GetErrorMessage().ToLower());
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange - Invalid extension AND too large
        var largeContent = new string('1', 11 * 1024 * 1024);
        var fileMock = CreateMockFile("data.pdf", largeContent, "application/pdf");

        // Act
        var result = CnabFileValidator.Validate(fileMock.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2); // At least 2 errors
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock IFormFile with specified properties.
    /// </summary>
    private Mock<IFormFile> CreateMockFile(string fileName, string content, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(bytes.Length);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        return fileMock;
    }

    #endregion
}
