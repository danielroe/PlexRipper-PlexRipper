using AutoMapper;
using Data.Contracts;
using DownloadManager.Contracts;

namespace PlexRipper.Data;

public class DataMappingProfile : Profile
{
    public DataMappingProfile()
    {
        CreateProjection<PlexMovie, Domain.PlexMedia>()
            .ForMember(x => x.Type, opt => opt.MapFrom(x => PlexMediaType.Movie))
            .ForMember(x => x.SortTitle, opt => opt.MapFrom(x => x.SortTitle ?? x.Title));
        CreateProjection<PlexTvShow, Domain.PlexMedia>()
            .ForMember(x => x.Type, opt => opt.MapFrom(x => PlexMediaType.TvShow))
            .ForMember(x => x.SortTitle, opt => opt.MapFrom(x => x.SortTitle ?? x.Title));
        CreateProjection<PlexTvShowSeason, Domain.PlexMedia>()
            .ForMember(x => x.Type, opt => opt.MapFrom(x => PlexMediaType.Season))
            .ForMember(x => x.SortTitle, opt => opt.MapFrom(x => x.SortTitle ?? x.Title));
        CreateProjection<PlexTvShowEpisode, Domain.PlexMedia>()
            .ForMember(x => x.Type, opt => opt.MapFrom(x => PlexMediaType.Episode))
            .ForMember(x => x.SortTitle, opt => opt.MapFrom(x => x.SortTitle ?? x.Title));

        CreateProjection<PlexMovie, PlexMediaSlim>()
            .ForMember(x => x.Type, opt => opt.MapFrom(x => PlexMediaType.Movie))
            .ForMember(x => x.SortTitle, opt => opt.MapFrom(x => x.SortTitle ?? x.Title))
            .ForMember(x => x.Qualities, opt => opt.Ignore());
        CreateProjection<PlexTvShow, PlexMediaSlim>()
            .ForMember(x => x.Type, opt => opt.MapFrom(x => PlexMediaType.TvShow))
            .ForMember(x => x.SortTitle, opt => opt.MapFrom(x => x.SortTitle ?? x.Title))
            .ForMember(x => x.Qualities, opt => opt.Ignore());
        CreateProjection<PlexTvShowSeason, PlexMediaSlim>()
            .ForMember(x => x.Type, opt => opt.MapFrom(x => PlexMediaType.Season))
            .ForMember(x => x.SortTitle, opt => opt.MapFrom(x => x.SortTitle ?? x.Title))
            .ForMember(x => x.Qualities, opt => opt.Ignore());
        CreateProjection<PlexTvShowEpisode, PlexMediaSlim>()
            .ForMember(x => x.Type, opt => opt.MapFrom(x => PlexMediaType.Episode))
            .ForMember(x => x.SortTitle, opt => opt.MapFrom(x => x.SortTitle ?? x.Title))
            .ForMember(x => x.Qualities, opt => opt.Ignore());

        CreateProjection<PlexMovie, DownloadPreview>()
            .ForMember(x => x.Children, opt => opt.Ignore())
            .ForMember(x => x.TvShowId, opt => opt.Ignore())
            .ForMember(x => x.SeasonId, opt => opt.Ignore())
            .ForMember(x => x.Size, opt => opt.MapFrom(x => x.MediaSize))
            .ForMember(x => x.MediaType, opt => opt.MapFrom(x => PlexMediaType.Movie));
        CreateProjection<PlexTvShow, DownloadPreview>()
            .ForMember(x => x.Children, opt => opt.Ignore())
            .ForMember(x => x.TvShowId, opt => opt.Ignore())
            .ForMember(x => x.SeasonId, opt => opt.Ignore())
            .ForMember(x => x.Size, opt => opt.MapFrom(x => x.MediaSize))
            .ForMember(x => x.MediaType, opt => opt.MapFrom(x => PlexMediaType.TvShow));
        CreateProjection<PlexTvShowSeason, DownloadPreview>()
            .ForMember(x => x.Children, opt => opt.Ignore())
            .ForMember(x => x.SeasonId, opt => opt.Ignore())
            .ForMember(x => x.TvShowId, opt => opt.MapFrom(x => x.TvShowId))
            .ForMember(x => x.Size, opt => opt.MapFrom(x => x.MediaSize))
            .ForMember(x => x.MediaType, opt => opt.MapFrom(x => PlexMediaType.Season));
        CreateProjection<PlexTvShowEpisode, DownloadPreview>()
            .ForMember(x => x.Children, opt => opt.Ignore())
            .ForMember(x => x.TvShowId, opt => opt.MapFrom(x => x.TvShowId))
            .ForMember(x => x.SeasonId, opt => opt.MapFrom(x => x.TvShowSeasonId))
            .ForMember(x => x.Size, opt => opt.MapFrom(x => x.MediaSize))
            .ForMember(x => x.MediaType, opt => opt.MapFrom(x => PlexMediaType.Episode));

        CreateProjection<PlexTvShowEpisode, TvShowEpisodeKeyDTO>()
            .ForMember(x => x.TvShowId, opt => opt.MapFrom(x => x.TvShowId))
            .ForMember(x => x.SeasonId, opt => opt.MapFrom(x => x.TvShowSeasonId))
            .ForMember(x => x.EpisodeId, opt => opt.MapFrom(x => x.Id));
    }
}