set quiet

root_folder := "src"
solution := root_folder / "SourceGeneratorFramework.slnx"
build_configuration := "Release"
artifacts_folder := "./artifacts"
default_test_filter := "/*/*/*/*/"
current_version := `node -p "require('./package.json').version"`

[private]
default:
    just --list

# Build and test with the specified configuration, defaulting to "Release"
build solutionOrProject=solution configuration=build_configuration:
    echo "Building {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }}"
    dotnet build {{ solutionOrProject }} -c {{ configuration }}

# Run tests with the specified configuration, defaulting to "Release"
tests solutionOrProject=solution configuration=build_configuration filter=default_test_filter *args:
    echo "Running tests for {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }} and filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test {{ solutionOrProject }} -c {{ configuration }} --treenode-filter "{{ filter }}" {{ args }}

# Run tests with the specified configuration, defaulting to "Release"
restore solutionOrProject=solution:
    echo "Restoring dependencies for {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }}"
    dotnet restore {{ solutionOrProject }}

# Create NuGet package for the project
pack solutionOrProject=solution configuration=build_configuration publish_folder=artifacts_folder:
    echo "Packing {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }} to {{ GREEN }}{{ publish_folder }}{{ NORMAL }}"
    dotnet pack {{ solutionOrProject }} -c {{ configuration }} -o {{ publish_folder }}

# Display the current version of the project
version:
    echo "Current version: {{ GREEN }}{{ current_version }}{{ NORMAL }}"

# Check code formatting using CSharpier
lint-check:
    dotnet csharpier check .
    # dotnet format --verify-no-changes {{ solution }}

# Fix code formatting issues using CSharpier
lint-fix:
    dotnet csharpier format .
    # dotnet format {{ solution }}

# Open the solution in Visual Studio/ Registered application
vs:
    open {{ solution }}
