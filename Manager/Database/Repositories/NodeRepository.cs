using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database.Repositories;

internal class NodeRepository : INodeRepository
{
    private readonly INodeDataContext _context;

    public NodeRepository(INodeDataContext context)
    {
        _context = context;
    }
    
    public void CreateOrUpdateNode(string name, Uri address)
    {
        var existingNode = _context.Nodes.SingleOrDefault(node => node.Name == name);
        if (existingNode is not null)
        {
            existingNode.Address = address;
        }
        else
        {
            var noteEntity = new NodeEntity
            {
                Name = name,
                Address = address,
            };
            _context.Nodes.Add(noteEntity);
        }
        _context.SaveChanges();
    }

    public List<NodeDto> GetAllNodes()
    {
        return _context.Nodes
            .Select(node => new NodeDto(node.Id, node.Name, node.Address.ToString()))
            .ToList();
    }

    public NodeDto GetNodeById(int nodeId)
    {
        return _context.Nodes
            .Where(entity => entity.Id == nodeId)
            .Select(node => new NodeDto(
                node.Id,
                node.Name,
                node.Address.ToString()
            ))
            .Single();
    }
}