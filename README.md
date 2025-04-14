# 1. Setup
- Port fe: http://localhost:5173
- Be: http://localhost:5028
- Download dotnet-ef: dotnet tool install --global dotnet-ef
- Update dotnet-ef: dotnet tool update --global dotnet-ef
# 1.1 Database and MinIO
- docker-compose up -d # Install docker
# 1.1.1 PostgresDB
- cd Docker/PostgresDB
# 1.1.2 MinIO
- cd Docker/MinIO
- localhost:9000
- user:     myrootuser
- password: myrootpassword
- Administrator/Buckets/Create Bucket
- Named bucket: file
- User/Access Keys/Create Access Key 
- "accessKey":"td45L84nRQkNhXUs9THA","secretKey":"8PUR3vqC2oXqrSu2HjHLm5h6I46oA9Bk5FAgB1Ao",
# 1.2 Build and run
- dotnet ef migrations add InitialCreate
- dotnet ef database update
- dotnet ef database drop # if database broken
- dotnet run (do not build)
# 2. Api
- Read it: https://axios-http.com/docs/intro
- Add bruno file, all api setting on there
# 3. Some tips in docker/README.me 
# II. Setup vps
- sudo dnf --refresh update
- sudo dnf update
- sudo dnf install yum-utils
# 1. Package dotnet
- sudo dnf install dotnet-sdk-9.0
- dotnet tool install --global dotnet-ef
- export PATH="$PATH:$HOME/.dotnet/tools"
# 2. Docker
#!/bin/bash

# Update the system
sudo dnf update -y

# Install Docker
sudo dnf config-manager --add-repo=https://download.docker.com/linux/centos/docker-ce.repo
sudo dnf install -y docker-ce docker-ce-cli containerd.io

# Start and enable Docker
sudo systemctl start docker
sudo systemctl enable docker

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/download/1.29.2/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Verify Docker and Docker Compose installations
docker --version
docker-compose --version
sudo nano /etc/systemd/system/server.service

[Unit]
Description=ASP.NET Core App - EngStu
After=network.target

[Service]
WorkingDirectory=/var/www/server
ExecStart=/usr/bin/dotnet /var/www/server/backend.dll
Restart=always
RestartSec=10
User=root
Environment=ASPNETCORE_URLS=http://0.0.0.0:5028
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target

sudo systemctl daemon-reload
sudo systemctl enable server
sudo systemctl start server
sudo systemctl status server

sudo yum install nginx -y

sudo nano /etc/nginx/conf.d/server.conf

server {
    listen 80;
    server_name 42.96.13.119;

    location / {
        proxy_pass         http://localhost:5028;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}





