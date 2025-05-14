namespace UserFlow.API.Shared.DTO;

public interface IEntityDTO<TId>
{
    TId Id { get; set; }
}