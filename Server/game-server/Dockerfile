# game-server Dockerfile — 多模块构建
# Build context: Server/ (父目录)

# ─── 构建阶段 ───
FROM maven:3.9-eclipse-temurin-21 AS builder

WORKDIR /build
# 先复制所有 pom（利用 Docker 层缓存）
COPY pom.xml .
COPY network/pom.xml network/pom.xml
COPY game-server/pom.xml game-server/pom.xml
COPY account-server/pom.xml account-server/pom.xml
# 预取依赖（game-server 依赖 network）
RUN mvn -pl game-server -am dependency:go-offline -B || true

# 复制源码并构建（-am 同时构建 network 依赖）
COPY network/src network/src
COPY game-server/src game-server/src
RUN mvn -pl game-server -am clean package -DskipTests -B

# ─── 运行阶段 ───
FROM eclipse-temurin:21-jre-jammy

WORKDIR /app
COPY --from=builder /build/game-server/target/*.jar app.jar

EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
