# account-server Dockerfile — 多阶段构建：容器内 Maven 编译，最终镜像仅含 JRE + jar
# Build context: Server/ (父目录)
# 部署: git pull 后直接 docker compose up -d --build，无需本地安装 Maven/JDK

# ========== 阶段1: Maven 构建（临时阶段，不进最终镜像） ==========
FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /src

# 阿里云镜像加速依赖下载
COPY maven-settings.xml /root/.m2/settings.xml

# 先只拷贝 pom：源码变动不会破坏依赖缓存层
COPY pom.xml .
COPY network/pom.xml network/pom.xml
COPY game-server/pom.xml game-server/pom.xml
COPY account-server/pom.xml account-server/pom.xml

# 预热依赖（go-offline 对聚合工程偶有解析不全，失败不阻塞，后续 package 会补齐）
RUN --mount=type=cache,target=/root/.m2/repository \
    mvn -B -s /root/.m2/settings.xml dependency:go-offline || true

# 再拷贝源码编译（account-server 不依赖 network，无需 -am）
COPY account-server/src account-server/src
RUN --mount=type=cache,target=/root/.m2/repository \
    mvn -B -s /root/.m2/settings.xml -pl account-server package -DskipTests

# ========== 阶段2: JRE 运行（最终镜像，约 250MB） ==========
FROM eclipse-temurin:21-jre-jammy
WORKDIR /app
COPY --from=build /src/account-server/target/*.jar app.jar

# 容器感知的内存上限（配合 compose mem_limit，JVM 取容器内存的 75% 作堆）
ENV JAVA_OPTS="-XX:MaxRAMPercentage=75.0"

EXPOSE 8081
ENTRYPOINT ["sh", "-c", "java $JAVA_OPTS -jar app.jar"]
