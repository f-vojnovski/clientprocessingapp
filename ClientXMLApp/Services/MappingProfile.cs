using ClientXMLApp.Models;
using ClientXMLApp.Services.DTOs;
using AutoMapper;

namespace ClientXMLApp.Services
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Client, ViewClientDto>()
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses));

            CreateMap<ViewClientDto, Client>();
            CreateMap<AddClientDto, Client>()
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses));

            CreateMap<Address, AddressDto>().ReverseMap();

            CreateMap<XmlClient, AddClientDto>()
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.BirthDate))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            CreateMap<XmlAddress, AddressDto>()
                .ForMember(dest => dest.AddressText, opt => opt.MapFrom(src => src.AddressText))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (AddressType)src.Type));
        }
    }
}
