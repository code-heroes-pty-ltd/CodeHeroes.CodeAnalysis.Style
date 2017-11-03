//namespace QUT.ViewModels.Parking
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Collections.Immutable;
//    using System.Linq;
//    using System.Reactive;
//    using System.Reactive.Concurrency;
//    using System.Reactive.Disposables;
//    using System.Reactive.Linq;
//    using Campus;
//    using Genesis.Ensure;
//    using Genesis.Logging;
//    using ReactiveUI;
//    using Services.Campus;
//    using Services.Mapping;
//    using Services.ViewStack;
//    using Services.WebApi;
//    using Utility.Mapping;

//    public sealed class ParkingViewModel : ActivatableReactiveObject, IPageViewModel
//    {
//        private readonly CarParkViewModelFactory carParkViewModelFactory;
//        private readonly ICampusService campusService;
//        private readonly CampusViewModelFactory campusViewModelFactory;
//        private readonly ReactiveCommand<Unit, LoadInfo> loadCommand;
//        private readonly ReactiveCommand<Unit, Unit> toggleCampusCommand;
//        private readonly ReactiveCommand<Unit, Unit> toggleViewModeCommand;
//        private readonly ObservableAsPropertyHelper<LoadInfo> currentLoadInfo;
//        private readonly ObservableAsPropertyHelper<IImmutableList<CampusViewModel>> campuses;
//        private readonly ObservableAsPropertyHelper<IImmutableList<CarParkViewModel>> carParks;
//        private readonly ObservableAsPropertyHelper<IImmutableList<CarParkViewModel>> campusCarParks;
//        private readonly ObservableAsPropertyHelper<Result> result;
//        private readonly ObservableAsPropertyHelper<ViewMode> viewMode;
//        private readonly IWebApiService webApiService;
//        private CampusViewModel selectedCampus;
//        private CarParkViewModel selectedCarPark;

//        public ParkingViewModel(
//            ICampusService campusService,
//            CampusViewModelFactory campusViewModelFactory,
//            CarParkViewModelFactory carParkViewModelFactory,
//            TimeSpan locationTimeout,
//            IScheduler mainScheduler,
//            IMappingService mappingService,
//            TimeSpan refreshInterval,
//            IScheduler timerScheduler,
//            IViewStackService viewStackService,
//            IWebApiService webApiService)
//        {
//            Ensure.ArgumentNotNull(campusService, nameof(campusService));
//            Ensure.ArgumentNotNull(campusViewModelFactory, nameof(campusViewModelFactory));
//            Ensure.ArgumentNotNull(carParkViewModelFactory, nameof(carParkViewModelFactory));
//            Ensure.ArgumentNotNull(mainScheduler, nameof(mainScheduler));
//            Ensure.ArgumentNotNull(mappingService, nameof(mappingService));
//            Ensure.ArgumentNotNull(timerScheduler, nameof(timerScheduler));
//            Ensure.ArgumentNotNull(viewStackService, nameof(viewStackService));
//            Ensure.ArgumentNotNull(webApiService, nameof(webApiService));

//            var logger = LoggerService.GetLogger(this.GetType());

//            using (logger.Perf("Construction."))
//            {
//                this.campusService = campusService;
//                this.campusViewModelFactory = campusViewModelFactory;
//                this.carParkViewModelFactory = carParkViewModelFactory;
//                this.webApiService = webApiService;

//                var lastKnownLocation = Observable
//                    .Timer(TimeSpan.Zero, refreshInterval, timerScheduler)
//                    .Select(
//                        _ =>
//                            mappingService
//                                .GetLocation()
//                                .Timeout(locationTimeout, timerScheduler)
//                                .Catch<Geolocation, Exception>(
//                                    ex =>
//                                    {
//                                        logger.Warn(ex, "Failed to obtain current location.");
//                                        return Observable<Geolocation>.Empty;
//                                    }))
//                    .Switch()
//                    .Do(_ => logger.Debug("Last known location changed to {0}.", _))
//                    .Select(geolocation => (Geolocation?)geolocation)
//                    .StartWith((Geolocation?)null);

//                this.loadCommand = ReactiveCommand
//                    .CreateFromObservable(
//                        this.Load,
//                        outputScheduler: mainScheduler);

//                this.toggleCampusCommand = ReactiveCommand
//                    .Create(
//                        () =>
//                        {
//                            if (this.SelectedCampus == this.Campuses[0])
//                            {
//                                this.SelectedCampus = this.Campuses[1];
//                            }
//                            else
//                            {
//                                this.SelectedCampus = this.Campuses[0];
//                            }
//                        },
//                        outputScheduler: mainScheduler);

//                this.toggleViewModeCommand = ReactiveCommand
//                    .Create(
//                        () => { },
//                        outputScheduler: mainScheduler);

//                this.viewMode = this
//                    .toggleViewModeCommand
//                    .Scan(ViewMode.List, (acc, _) => acc.Toggle())
//                    .StartWith(ViewMode.List)
//                    .ToProperty(this, x => x.ViewMode);

//                this.currentLoadInfo = this
//                    .loadCommand
//                    .ToProperty(this, c => c.CurrentLoadInfo);
//                this.campuses = this
//                    .WhenAnyValue(x => x.CurrentLoadInfo)
//                    .Select(currentLoadInfo => currentLoadInfo?.Campuses)
//                    .ToProperty(this, x => x.Campuses);
//                this.carParks = this
//                    .GetIsActivated()
//                    .Select(
//                        isActivated =>
//                        {
//                            if (!isActivated)
//                            {
//                                return Observable<ImmutableList<CarParkViewModel>>.Empty;
//                            }
//                            else
//                            {
//                                var carParks = this
//                                    .WhenAnyValue(x => x.CurrentLoadInfo)
//                                    .Select(currentLoadInfo => currentLoadInfo?.CarParks ?? ImmutableList<CarParkViewModel>.Empty);

//                                return Observable
//                                    .CombineLatest(
//                                        carParks,
//                                        lastKnownLocation.ObserveOn(mainScheduler),
//                                        (parks, location) =>
//                                            parks
//                                                .Select(a => a.WithCurrentLocation(location))
//                                                .OrderBy(x => x, CarParkViewModelComparer.Instance)
//                                                .ToImmutableList());
//                            }
//                        })
//                    .Switch()
//                    .ToProperty(this, x => x.CarParks);
//                this.campusCarParks = Observable
//                    .CombineLatest(
//                        this.WhenAnyValue(x => x.CarParks),
//                        this.WhenAnyValue(x => x.SelectedCampus),
//                        (_, selectedCampus) =>
//                            _
//                                ?.Where(carPark => carPark.CampusId == selectedCampus?.Id)
//                                .ToImmutableList() ?? ImmutableList<CarParkViewModel>.Empty)
//                    .ToProperty(this, x => x.CampusCarParks);

//                this.result = Observable
//                    .Merge(
//                        this
//                            .loadCommand
//                            .ThrownExceptions
//                            .Select(ex => new Error(ex, this.loadCommand)),
//                        this
//                            .loadCommand
//                            .Select(_ => (Result)null))
//                    .ToProperty(this, x => x.Result);

//                this
//                    .WhenAnyValue(x => x.Campuses)
//                    .Where(campuses => campuses != null && this.SelectedCampus == null)
//                    .Select(this.DetermineSelectedCampus)
//                    .Do(selectedCampus => this.SelectedCampus = selectedCampus)
//                    .SubscribeSafe();
//                this
//                    .WhenAnyValue(x => x.SelectedCarPark)
//                    .WhereNotNull()
//                    .SelectMany(selectedCarPark => viewStackService.PushPage(selectedCarPark, "details"))
//                    .SubscribeSafe();

//                this
//                    .Activator
//                    .Activated
//                    .FirstAsync()
//                    .InvokeCommand(this.loadCommand);

//                this
//                    .WhenActivated(
//                        disposables =>
//                        {
//                            using (logger.Perf("Activation."))
//                            {
//                                logger.Debug("Activated.");
//                                Disposable
//                                    .Create(() => logger.Debug("Deactivated."))
//                                    .AddTo(disposables);

//                                this.SelectedCarPark = null;

//                                Observable
//                                    .Timer(refreshInterval, refreshInterval, timerScheduler)
//                                    .Where(_ => this.SelectedCampus != null)
//                                    .ToSignal()
//                                    .InvokeCommand(this.loadCommand)
//                                    .AddTo(disposables);
//                            }
//                        });
//            }
//        }

//        public string Id => "parking";

//        public ReactiveCommand<Unit, LoadInfo> LoadCommand => this.loadCommand;

//        public ReactiveCommand<Unit, Unit> ToggleCampusCommand => this.toggleCampusCommand;

//        public ReactiveCommand<Unit, Unit> ToggleViewModeCommand => this.toggleViewModeCommand;

//        public Result Result => this.result.Value;

//        public IImmutableList<CampusViewModel> Campuses => this.campuses.Value;

//        public IImmutableList<CarParkViewModel> CarParks => this.carParks.Value;

//        public IImmutableList<CarParkViewModel> CampusCarParks => this.campusCarParks.Value;

//        public CampusViewModel SelectedCampus
//        {
//            get => this.selectedCampus;
//            set => this.RaiseAndSetIfChanged(ref this.selectedCampus, value);
//        }

//        public CarParkViewModel SelectedCarPark
//        {
//            get => this.selectedCarPark;
//            set => this.RaiseAndSetIfChanged(ref this.selectedCarPark, value);
//        }

//        public ViewMode ViewMode => this.viewMode.Value;

//        private LoadInfo CurrentLoadInfo => this.currentLoadInfo.Value;

//        private IObservable<LoadInfo> Load() =>
//            Observable
//                .CombineLatest(
//                    this
//                        .campusService
//                        .GetCampuses()
//                        .Select(
//                            campuses =>
//                                campuses
//                                    .Where(campus => campus.Id != "CB" && campus.Id != "OC")
//                                    .Select(campus => this.campusViewModelFactory(campus))
//                                    .ToImmutableList()),
//                    this
//                        .webApiService
//                        .GetCarParks()
//                        .Select(
//                            carParks =>
//                                carParks
//                                    .Select(model => this.carParkViewModelFactory(model))
//                                    .ToImmutableList()),
//                    (campuses, carParks) => new LoadInfo(campuses, carParks));

//        private CampusViewModel DetermineSelectedCampus(IImmutableList<CampusViewModel> campuses) =>
//            campuses.FirstOrDefault();

//        public sealed class LoadInfo
//        {
//            private readonly IImmutableList<CampusViewModel> campuses;
//            private readonly IImmutableList<CarParkViewModel> carParks;

//            public LoadInfo(
//                IImmutableList<CampusViewModel> campuses,
//                IImmutableList<CarParkViewModel> carParks)
//            {
//                Ensure.ArgumentNotNull(campuses, nameof(campuses));
//                Ensure.ArgumentNotNull(carParks, nameof(carParks));

//                this.campuses = campuses;
//                this.carParks = carParks;
//            }

//            public IImmutableList<CampusViewModel> Campuses => this.campuses;

//            public IImmutableList<CarParkViewModel> CarParks => this.carParks;
//        }

//        private sealed class CarParkViewModelComparer : IComparer<CarParkViewModel>
//        {
//            public static readonly CarParkViewModelComparer Instance = new CarParkViewModelComparer();

//            private CarParkViewModelComparer()
//            {
//            }

//            public int Compare(CarParkViewModel first, CarParkViewModel second)
//            {
//                Ensure.ArgumentNotNull(first, nameof(first));
//                Ensure.ArgumentNotNull(second, nameof(second));

//                // Compare by distance first, with closer car parks taking precedence
//                var distanceCompare = first
//                    .Distance
//                    .GetValueOrDefault(double.MaxValue)
//                    .CompareTo(second.Distance.GetValueOrDefault(double.MaxValue));

//                if (distanceCompare != 0)
//                {
//                    return distanceCompare;
//                }

//                // If the distance is the same, compare by the number of available car parks
//                return second
//                    .Available
//                    .GetValueOrDefault()
//                    .CompareTo(first.Available.GetValueOrDefault());
//            }
//        }
//    }
//}