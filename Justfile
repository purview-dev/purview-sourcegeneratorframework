set quiet

root_folder := "src"
solution := root_folder / "SourceGeneratorFramework.slnx"
build_configuration := "Release"
artifacts_folder := "./artifacts"
default_test_filter := "/*/*/*/*/"
pipeline_solution := "build/Pipeline.slnx"
pipeline_project := "build/PipelineCLI/PipelineCLI.csproj"

current_version := `node -p "require('./package.json').version"`

[private]
default:
    just --list

# Run the PR pipeline (restore, build, lint, tests)
[group('Pipeline')]
pipeline-pr *args:
    echo "Running PR pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} {{ args }}

# Run the build pipeline (restore, build, lint)
[group('Pipeline')]
pipeline-build *args:
    echo "Running build pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Build:RunTests=false --Release:Mode=None {{ args }} 

# Run the release pipeline (restore, build, lint, tests, pack, publish, GitHub release)
[group('Pipeline')]
pipeline-release *args:
    echo "Running release pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Release:Mode=NuGet {{ args }}

# Run the release pipeline (restore, build, lint, tests, pack, local nuget publish)
# Note: when running via a sh-style shell on Windows, backslashes in the path may be stripped.
# Use forward slashes for the feed path, e.g. --PublishLocalNuGet:LocalFeedPath=p:/_sync-projects/.local-nuget/
[group('Pipeline')]
pipeline-local-release *args:
    echo "Running local release pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Release:Mode=LocalNuGet {{ args }}

# Run the pipeline with tests enabled
[group('Pipeline')]
pipeline-tests *args:
    echo "Running tests pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Build:RunTests=true --Release:Mode=None {{ args }}

# Build and test with the specified configuration, defaulting to "Release"
[group('Build and Test')]
build solutionOrProject=solution configuration=build_configuration:
    echo "Building {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }}"
    dotnet build {{ solutionOrProject }} -c {{ configuration }}

# Run tests with the specified configuration, defaulting to "Release"
[group('Build and Test')]
test solutionOrProject=solution configuration=build_configuration filter=default_test_filter *args:
    echo "Running tests for {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }} and filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test {{ solutionOrProject }} -c {{ configuration }} --treenode-filter "{{ filter }}" {{ args }}

# Clean all projects with the specified configuration, defaulting to "Release"
[group('Build and Test')]
clean solutionOrProject=solution configuration=build_configuration *args:
    echo "Cleaning {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }}"
    dotnet clean {{ solutionOrProject }} -c {{ configuration }} {{ args }}

# Clean all projects, across Debug and Release configurations
[group('Build and Test')]
clean-all *args:
    echo "Cleaning all projects with configuration"
    dotnet clean {{ solution }} -c Release {{ args }}
    dotnet clean {{ solution }} -c Debug {{ args }}

# Run tests with the specified configuration, defaulting to "Release"
[group('Build and Test')]
restore solutionOrProject=solution:
    echo "Restoring dependencies for {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }}"
    dotnet restore {{ solutionOrProject }}

# Create NuGet package for the project
[group('Build and Test')]
pack solutionOrProject=solution configuration=build_configuration publish_folder=artifacts_folder:
    echo "Packing {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }} to {{ GREEN }}{{ publish_folder }}{{ NORMAL }}"
    dotnet pack {{ solutionOrProject }} -c {{ configuration }} -o {{ publish_folder }}

# Display the current version of the project
[group('Build and Test')]
version:
    echo "Current version: {{ GREEN }}{{ current_version }}{{ NORMAL }}"

# Check code formatting using CSharpier
[group('Utilities')]
lint-check:
    dotnet csharpier check .
    # dotnet format --verify-no-changes {{ solution }}

# Fix code formatting issues using CSharpier
[group('Utilities')]
lint-fix:
    dotnet csharpier format .
    # dotnet format {{ solution }}

# Open the solution in Visual Studio/ Registered application
[group('Utilities')]
vs:
    open {{ solution }}

# Open the solution in Visual Studio/ Registered application
[group('Utilities')]
vs-pipeline:
    open {{ pipeline_solution }}
