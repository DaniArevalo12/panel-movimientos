using PruebaTecnica.Web.Common;

namespace PruebaTecnica.Web.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_EstableceIsSuccessYValue_YDejaErrorMessageEnNull()
    {
        var resultado = Result<int>.Success(42);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(42, resultado.Value);
        Assert.Null(resultado.ErrorMessage);
    }

    [Fact]
    public void Failure_EstableceIsSuccessEnFalseYErrorMessage_YDejaValueEnDefault()
    {
        var resultado = Result<int>.Failure("algo salió mal");

        Assert.False(resultado.IsSuccess);
        Assert.Equal(0, resultado.Value);
        Assert.Equal("algo salió mal", resultado.ErrorMessage);
    }

    [Fact]
    public void Failure_ConTipoReferencia_DejaValueEnNull()
    {
        var resultado = Result<string>.Failure("error");

        Assert.False(resultado.IsSuccess);
        Assert.Null(resultado.Value);
    }
}
