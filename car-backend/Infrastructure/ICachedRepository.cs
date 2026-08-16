using Models;

namespace Infrastructure;

public interface ICachedRepository<T> : IRepository<T> where T : EntityBase
{
}