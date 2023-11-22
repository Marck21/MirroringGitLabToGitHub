GITLAB_USERNAME = "GITLAB_USERNAME"
GITLAB_TOKEN = "GITLAB_TOKEN"

GITHUB_TOKEN = "GITHUB_TOKEN"

SELF_HOSTED_URL = "git.YOUR_SELF_HOSTED_NAME.app"
ORGANIZATION = "ORGANIZATION"

# Remember to install with -> pip install requests
import requests
import json

# Get repos list from GitLab self-hosted
response = requests.get(
    url='https://{selfhostedurl}/api/v4/projects?owned=true&per_page=100'.format(selfhostedurl=SELF_HOSTED_URL),
    headers={'PRIVATE-TOKEN': GITLAB_TOKEN})

# Get repos list from GitLab user
# response = requests.get( \
    # url='https://gitlab.com/api/v4/users/{user}/projects'.format(user=GITLAB_USERNAME), \
    # headers={'PRIVATE-TOKEN': GITLAB_TOKEN})

print(response)

success_project_list = []
error_project_list = []

for repo in json.loads(response.content):
    project = {
        'id': repo['id'],
        'name': repo['name'],
        # This namespace is used like the name after for mantain the group structure from GitLab to GitHub
        'namespace': repo['path_with_namespace'].replace('appia/', '').replace('clients', 'integraciones').replace('/', '_'),
        'weburl': repo['web_url'],
        'httpurl': repo['http_url_to_repo'],
        'sshurl': repo['ssh_url_to_repo'],
        'description': repo['description'],
        'visibility': repo['visibility']
    }
    
    # create GitHub repo into organization
    y = requests.post(
        url=f'https://api.github.com/orgs/{ORGANIZATION}/repos', # url='https://api.github.com/user/repos',
        headers={'Accept': 'application/vnd.github.v3+json', 'Authorization': 'token {token}'.format(token=GITHUB_TOKEN)},
        json={'name': project['namespace'], 'description': project['description'], 'private': project['visibility'] == 'private'}
    )
    # create GitHub repo into user
    # y = requests.post(
        # url='https://api.github.com/user/repos',
        # headers={'Accept': 'application/vnd.github.v3+json', 'Authorization': 'token {token}'.format(token=GITHUB_TOKEN)},
        # json={'name': project['namespace'], 'description': project['description'], 'private': project['visibility'] == 'private'}
    # )

    print(y)
    if y.status_code != 201:
        error_project_list.append(project)
        try:
            print(y.json())
        except ValueError:
            print("Response content is not in JSON format.")
    else:
        success_project_list.append(project)

with open('output.json', 'w') as file:
    json.dump({'success': success_project_list, 'error': error_project_list}, file)