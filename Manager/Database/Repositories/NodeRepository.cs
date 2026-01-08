using Microsoft.EntityFrameworkCore;
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
    
    public void CreateOrUpdateNode(NodeDto nodeDto)
    {
        var existingNode = _context.Nodes.SingleOrDefault(node => node.Name == nodeDto.Name);
        if (existingNode is not null)
        {
            existingNode.Address = nodeDto.Address;
        }
        else
        {
            var noteEntity = new NodeEntity
            {
                Name = nodeDto.Name,
                Address = nodeDto.Address,
            };
            _context.Nodes.Add(noteEntity);
        }
        _context.SaveChanges();
    }

    public List<NodeDto> GetAllNodes()
    {
        return _context.Nodes
            .Select(node => new NodeDto(node.Name, node.Address))
            .ToList();
    }
}