# LanCache DNS Sync

LanCache DNS Sync designed to automate the synchronization of LanCache DNS entries. It fetches DNS records daily, used by LanCache, and updates the local DNS server (AdGuard Home) configuration.
It serves users who already have a running local DNS server (AdGuard Home) in their LAN and wish to use that server to resolve DNS queries for LanCache, instead of using the default LanCache-DNS container.

## Usage

Demonstration of how to use LanCache DNS Sync with AdGuard Home and LanCache Monolithic cache server. Adapt the `docker-compose.yml` file to your environment, ensuring that the IP addresses and paths match your setup.

Before starting the stack, create a `config.json` (based on [`scripts/config.example.json`](scripts/config.example.json)) and place it where it will be mounted into the `lancache-dns-sync` container, e.g. `${APPDATA_PATH}/LanCacheDnsSync/config.json`. It controls the cache-server IP(s) per CDN group and whether a single combined rule file (`combined_output: true`) or one file per CDN (`combined_output: false`) is produced.


```yaml
services:
  adguardhome:
    image: adguard/adguardhome:latest
    container_name: adguardhome
    ports:
      - 192.168.0.3:3000:3000/tcp # initial setup web interface
      - 192.168.0.3:53:53/tcp # plain dns over tcp
      - 192.168.0.3:53:53/udp # plain dns over udp
      - 192.168.0.3:80:80/tcp # http web interface
      #- 443:443/tcp # Add if you are going to run AdGuard Home as an HTTPS/DNS-over-HTTPS⁠ server.
      #- 443:443/udp # Add if you are going to run AdGuard Home as an HTTPS/DNS-over-HTTPS⁠ server.
    volumes:
      - ${APPDATA_PATH}/AdGuardHome/config:/opt/adguardhome/conf # app configuration
      - ${APPDATA_PATH}/AdGuardHome/work:/opt/adguardhome/work # app working directory
    networks:
      lan-net:
         ipv4_address: 192.168.0.3

  monolithic:
    image: lancachenet/monolithic:latest
    container_name: lancache
    environment:
      - CACHE_DISK_SIZE=${CACHE_DISK_SIZE}
      - CACHE_INDEX_SIZE=${CACHE_INDEX_SIZE}
      - MIN_FREE_DISK=${MIN_FREE_DISK}
      - CACHE_MAX_AGE=${CACHE_MAX_AGE}
      - UPSTREAM_DNS=${UPSTREAM_DNS}
      - CACHE_SLICE_SIZE=${CACHE_SLICE_SIZE}
    ports:
      - 192.168.0.4:80:80/tcp
      - 192.168.0.4:443:443/tcp
    volumes:
      - ${APPDATA_PATH}/LanCache/cache:/data/cache
      - ${APPDATA_PATH}/LanCache/logs:/data/logs
    networks:
      lan-net:
         ipv4_address: 192.168.0.4

  lancache-dns-sync:
    image: adamradocz/lancache-dns-sync:latest
    container_name: lancache-dns-sync
    environment:
      - PUID=1000
      - PGID=1000
      - CACHE_DOMAINS_REPO=https://github.com/uklans/cache-domains.git
    volumes:  
      - ${APPDATA_PATH}/LanCacheDnsSync/data:/data
      - ${APPDATA_PATH}/AdGuardHome/work/userfilters:/userfilters
    networks:
      - private-net

# Network configuration
networks:

  # Direct LAN access without NAT.
  lan-net:
    name: lan-net
    driver: ipvlan
    driver_opts:
      parent: enp1s0 # In the driver options the parent must be the physical interface.
    ipam:
      config:
        - subnet: 192.168.0.0/24
          gateway: 192.168.0.1

  private-net:
    name: private-net
    driver: bridge
    ipam:
      config:
        - subnet: 172.21.0.0/24
```


### Environment Variables
| Variable            | Description                                                                                       | Required | Default                                     |
|---------------------|---------------------------------------------------------------------------------------------------|----------|---------------------------------------------|
| PUID                | User ID under which the update scripts and rule generation run. Matches host user permissions.    | No       | 1000                                        |
| PGID                | Group ID under which the update scripts and rule generation run. Matches host group permissions.  | No       | 1000                                        |
| CACHE_DOMAINS_REPO  | Git URL of the cache-domains repository to clone. The repository's default branch is always used. | No       | https://github.com/uklans/cache-domains.git |

### Volumes
| Volume              | Description                                                                                             |
|---------------------|-----------------------------------------------------------------------------------------------------------|
| /data               | Persists the cloned `cache-domains` repository (`/data/cache-domains`) between container restarts.         |
| /data/config.json   | Your `config.json` (see `scripts/config.example.json`), mounted read-only. Controls IPs and combined/per-CDN output. |
| /userfilters        | Directory where the generated AdGuard rule file(s) (e.g. `lancache.txt`) are written. Map it to your AdGuard Home user filters directory. |

The container clones `cache-domains` and generates the rewrite rules immediately on startup, then again once per day at 02:00 (container local time) via an internal cron job (BusyBox `crond`).

## Repository Structure
```shell
📁                               # Root of the repository.
├─📁.github                      # GitHub workflows and templates.
│ └─📁workflows                  # CI/CD pipeline definitions.
├─📁scripts                      # Helper scripts for Docker.
│ ├─check-for-updates.sh         # Script to check for DNS rules updates.
│ ├─entrypoint.sh                # Entrypoint script for the Docker container.
│ └─update-dns-rewrite-rules.sh  # Script to update the DNS rewrite rules using the LanCacheDnsRewriteGen.
├─.gitignore                     # Ignore build artifacts, user secrets, etc.
├─LICENSE                        # Defines the legal terms under which others can use, modify, and distribute the code.
└─README.md                      # You're reading this right now.
```

## Used technologies & frameworks
- [Alpine Linux](https://alpinelinux.org/)
- [jq](https://jqlang.github.io/jq/)
- [Docker](https://www.docker.com/)
